using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScopeSeal.Billing.Configuration;
using ScopeSeal.Billing.Domain;
using ScopeSeal.Billing.Services;

namespace ScopeSeal.Infrastructure.Services;

public sealed class PaymentGatewayFactory(
    IOptions<BillingOptions> billingOptions,
    LocalTestPaymentGateway localTestGateway,
    RazorpayPaymentGateway razorpayGateway)
{
    public IPaymentGateway Resolve()
    {
        return billingOptions.Value.Mode switch
        {
            "LocalTest" => localTestGateway,
            "Razorpay" => razorpayGateway,
            _ => throw new InvalidOperationException("Billing is disabled.")
        };
    }
}

public sealed class LocalTestPaymentGateway(IOptions<BillingOptions> billingOptions) : IPaymentGateway
{
    private readonly BillingOptions _options = billingOptions.Value;

    public Task<PaymentCustomerResult> CreateOrGetCustomerAsync(
        PaymentCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = $"cust_test_{ComputeStableHash(request.TenantReference)[..16]}";
        return Task.FromResult(new PaymentCustomerResult(customerId));
    }

    public Task<PaymentSubscriptionResult> CreateSubscriptionAsync(
        PaymentSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = $"sub_test_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentSubscriptionResult(
            subscriptionId,
            "created",
            _options.Razorpay.KeyId));
    }

    public PaymentSignatureVerificationResult VerifyPaymentSignature(
        string subscriptionId,
        string paymentId,
        string signature)
    {
        var expected = ComputeHmac($"{subscriptionId}|{paymentId}", _options.Razorpay.KeySecret);
        return signature.Equals(expected, StringComparison.Ordinal)
            ? new PaymentSignatureVerificationResult(true, null)
            : new PaymentSignatureVerificationResult(false, "Invalid payment signature.");
    }

    public PaymentSignatureVerificationResult VerifyWebhookSignature(ReadOnlySpan<byte> rawBody, string signature)
    {
        var expected = ComputeHmac(rawBody, _options.Razorpay.WebhookSecret);
        if (signature.Equals(expected, StringComparison.Ordinal))
        {
            return new PaymentSignatureVerificationResult(true, null);
        }

        if (!string.IsNullOrWhiteSpace(_options.Razorpay.WebhookSecretPrevious))
        {
            var previous = ComputeHmac(rawBody, _options.Razorpay.WebhookSecretPrevious);
            if (signature.Equals(previous, StringComparison.Ordinal))
            {
                return new PaymentSignatureVerificationResult(true, null);
            }
        }

        return new PaymentSignatureVerificationResult(false, "Invalid webhook signature.");
    }

    public PaymentWebhookEvent ParseWebhookEvent(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
        var eventType = root.GetProperty("event").GetString() ?? "unknown";
        var payload = root.GetProperty("payload");
        var subscription = payload.TryGetProperty("subscription", out var subscriptionElement)
            ? subscriptionElement.GetProperty("entity")
            : default;
        var payment = payload.TryGetProperty("payment", out var paymentElement)
            ? paymentElement.GetProperty("entity")
            : default;

        var externalSubscriptionId = subscription.ValueKind == JsonValueKind.Object
            ? subscription.GetProperty("id").GetString()
            : null;
        var externalPaymentId = payment.ValueKind == JsonValueKind.Object
            ? payment.GetProperty("id").GetString()
            : null;

        return new PaymentWebhookEvent(
            eventId,
            eventType,
            externalSubscriptionId,
            externalPaymentId,
            MapEventType(eventType));
    }

    public Task CancelSubscriptionAsync(
        string externalSubscriptionId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<PaymentSubscriptionSnapshot?> GetSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PaymentSubscriptionSnapshot?>(new PaymentSubscriptionSnapshot(
            externalSubscriptionId,
            "active",
            null));
    }

    internal static SubscriptionStatus? MapEventType(string eventType) => eventType switch
    {
        "subscription.authenticated" => SubscriptionStatus.Authenticated,
        "subscription.activated" => SubscriptionStatus.Active,
        "subscription.charged" => SubscriptionStatus.Active,
        "subscription.pending" => SubscriptionStatus.Pending,
        "subscription.halted" => SubscriptionStatus.Halted,
        "subscription.paused" => SubscriptionStatus.Paused,
        "subscription.resumed" => SubscriptionStatus.Active,
        "subscription.cancelled" => SubscriptionStatus.Cancelled,
        "subscription.completed" => SubscriptionStatus.Completed,
        "payment.failed" => SubscriptionStatus.GracePeriod,
        _ => null
    };

    internal static string ComputeHmac(string payload, string secret) =>
        ComputeHmac(Encoding.UTF8.GetBytes(payload), secret);

    internal static string ComputeHmac(ReadOnlySpan<byte> payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(payload.ToArray());
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeStableHash(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class RazorpayPaymentGateway(
    IOptions<BillingOptions> billingOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<RazorpayPaymentGateway> logger) : IPaymentGateway
{
    private readonly BillingOptions _options = billingOptions.Value;
    private readonly LocalTestPaymentGateway _signatureHelper = new(billingOptions);

    public async Task<PaymentCustomerResult> CreateOrGetCustomerAsync(
        PaymentCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureTestModeOnly();
        var client = CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = request.Name,
            ["email"] = request.Email,
            ["notes[tenant_reference]"] = request.TenantReference
        });

        using var response = await client.PostAsync("customers", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Razorpay customer creation failed with status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Unable to create Razorpay customer.");
        }

        using var document = JsonDocument.Parse(body);
        var customerId = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Razorpay customer response missing id.");
        return new PaymentCustomerResult(customerId);
    }

    public async Task<PaymentSubscriptionResult> CreateSubscriptionAsync(
        PaymentSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureTestModeOnly();
        var client = CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["plan_id"] = request.RazorpayPlanId,
            ["customer_id"] = request.ExternalCustomerId,
            ["total_count"] = "120",
            ["quantity"] = "1",
            ["customer_notify"] = "1"
        }.Concat(request.Notes.Select(note => new KeyValuePair<string, string>($"notes[{note.Key}]", note.Value))));

        using var response = await client.PostAsync("subscriptions", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Razorpay subscription creation failed with status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Unable to create Razorpay subscription.");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        return new PaymentSubscriptionResult(
            root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Missing subscription id."),
            root.GetProperty("status").GetString() ?? "created",
            _options.Razorpay.KeyId);
    }

    public PaymentSignatureVerificationResult VerifyPaymentSignature(
        string subscriptionId,
        string paymentId,
        string signature) =>
        _signatureHelper.VerifyPaymentSignature(subscriptionId, paymentId, signature);

    public PaymentSignatureVerificationResult VerifyWebhookSignature(ReadOnlySpan<byte> rawBody, string signature) =>
        _signatureHelper.VerifyWebhookSignature(rawBody, signature);

    public PaymentWebhookEvent ParseWebhookEvent(string rawBody) =>
        _signatureHelper.ParseWebhookEvent(rawBody);

    public async Task CancelSubscriptionAsync(
        string externalSubscriptionId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken = default)
    {
        EnsureTestModeOnly();
        var client = CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["cancel_at_cycle_end"] = cancelAtCycleEnd ? "1" : "0"
        });
        using var response = await client.PostAsync($"subscriptions/{externalSubscriptionId}/cancel", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Razorpay subscription cancel failed for {SubscriptionId} with status {StatusCode}",
                externalSubscriptionId,
                response.StatusCode);
            throw new InvalidOperationException("Unable to cancel Razorpay subscription.");
        }
    }

    public async Task<PaymentSubscriptionSnapshot?> GetSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        EnsureTestModeOnly();
        var client = CreateClient();
        using var response = await client.GetAsync($"subscriptions/{externalSubscriptionId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        return new PaymentSubscriptionSnapshot(
            root.GetProperty("id").GetString() ?? externalSubscriptionId,
            root.GetProperty("status").GetString() ?? "unknown",
            root.TryGetProperty("plan_id", out var planId) ? planId.GetString() : null);
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("Razorpay");
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.Razorpay.KeyId}:{_options.Razorpay.KeySecret}"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        return client;
    }

    private void EnsureTestModeOnly()
    {
        if (!_options.TestModeOnly || !_options.Razorpay.KeyId.StartsWith("rzp_test_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Razorpay live mode is not enabled for ScopeSeal.");
        }
    }
}

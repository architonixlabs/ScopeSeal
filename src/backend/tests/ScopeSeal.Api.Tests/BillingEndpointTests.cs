using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Billing.Services;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class BillingEndpointTests(BillingPostgresWebApplicationFactory factory)
    : IClassFixture<BillingPostgresWebApplicationFactory>
{
    private const string WebhookSecret = "test_webhook_secret_for_signatures";
    private const string PaymentSecret = "test_key_secret_for_hmac_signatures";

    [Fact]
    public async Task CheckoutVerifyAndWebhookGrantProEntitlements()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"billing-{suffix}@example.com", "Billing Tenant");

        var checkoutResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/checkout",
            new { planCode = "Pro", interval = "Monthly" });
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var checkout = await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>();
        var subscriptionId = checkout.GetProperty("externalSubscriptionId").GetString()!;
        var paymentId = $"pay_test_{Guid.NewGuid():N}";
        var paymentSignature = ComputePaymentSignature(subscriptionId, paymentId, PaymentSecret);

        var verifyResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/verify-payment",
            new { subscriptionId, paymentId, signature = paymentSignature });
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var webhookBody = BuildWebhookPayload(
            $"evt_{Guid.NewGuid():N}",
            "subscription.activated",
            subscriptionId,
            paymentId);
        var webhookClient = factory.CreateClient();
        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/razorpay")
        {
            Content = new StringContent(webhookBody, Encoding.UTF8, "application/json")
        };
        webhookRequest.Headers.Add("X-Razorpay-Signature", ComputeWebhookSignature(webhookBody, WebhookSecret));

        var webhookResponse = await webhookClient.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var entitlements = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/entitlements");
        entitlements.GetProperty("plan").GetString().Should().Be("Pro");
        entitlements.GetProperty("source").GetString().Should().Be("WebSubscription");
    }

    [Fact]
    public async Task InvalidWebhookSignatureIsRejected()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"billing-bad-{suffix}@example.com", "Billing Bad Webhook");

        var checkoutResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/checkout",
            new { planCode = "Pro", interval = "Monthly" });
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var subscriptionId = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("externalSubscriptionId").GetString()!;

        var webhookBody = BuildWebhookPayload(
            $"evt_{Guid.NewGuid():N}",
            "subscription.activated",
            subscriptionId,
            $"pay_test_{Guid.NewGuid():N}");

        var webhookClient = factory.CreateClient();
        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/razorpay")
        {
            Content = new StringContent(webhookBody, Encoding.UTF8, "application/json")
        };
        webhookRequest.Headers.Add("X-Razorpay-Signature", "invalid_signature");

        var webhookResponse = await webhookClient.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReplayedWebhookIsIgnoredWithoutDoubleGrant()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"billing-replay-{suffix}@example.com", "Billing Replay");

        var checkoutResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/checkout",
            new { planCode = "Pro", interval = "Monthly" });
        var subscriptionId = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("externalSubscriptionId").GetString()!;

        var eventId = $"evt_{Guid.NewGuid():N}";
        var webhookBody = BuildWebhookPayload(
            eventId,
            "subscription.activated",
            subscriptionId,
            $"pay_test_{Guid.NewGuid():N}");
        var signature = ComputeWebhookSignature(webhookBody, WebhookSecret);

        var webhookClient = factory.CreateClient();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/razorpay")
            {
                Content = new StringContent(webhookBody, Encoding.UTF8, "application/json")
            };
            webhookRequest.Headers.Add("X-Razorpay-Signature", signature);
            var response = await webhookClient.SendAsync(webhookRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var entitlements = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/entitlements");
        entitlements.GetProperty("plan").GetString().Should().Be("Pro");
        entitlements.GetProperty("source").GetString().Should().Be("WebSubscription");
    }

    [Fact]
    public async Task TamperedPaymentSignatureIsRejected()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"billing-tamper-{suffix}@example.com", "Billing Tamper");

        var checkoutResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/checkout",
            new { planCode = "Pro", interval = "Monthly" });
        var subscriptionId = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("externalSubscriptionId").GetString()!;

        var verifyResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/verify-payment",
            new
            {
                subscriptionId,
                paymentId = "pay_test_tampered",
                signature = "deadbeef"
            });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelSubscriptionDowngradesToFreePlan()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"billing-cancel-{suffix}@example.com", "Billing Cancel");

        var checkoutResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/checkout",
            new { planCode = "Pro", interval = "Monthly" });
        var subscriptionId = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("externalSubscriptionId").GetString()!;

        await SendWebhookAsync(subscriptionId, "subscription.activated");

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/cancel",
            new { cancelAtCycleEnd = false });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var entitlements = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/entitlements");
        entitlements.GetProperty("plan").GetString().Should().Be("Free");
        entitlements.GetProperty("source").GetString().Should().Be("FreePlan");
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsBillingStatus()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var ownerClient = CreateAuthenticatedClient();
        await RegisterAndLoginAsync(ownerClient, $"billing-owner-{suffix}@example.com", "Owner Tenant");
        var ownerMe = await ownerClient.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var ownerTenantPublicId = ownerMe.GetProperty("tenant").GetProperty("publicId").GetGuid();

        var intruderClient = CreateAuthenticatedClient();
        await RegisterAndLoginAsync(intruderClient, $"billing-intruder-{suffix}@example.com", "Intruder Tenant");

        var response = await intruderClient.GetAsync(
            $"/api/v1/tenants/{ownerTenantPublicId}/billing/status");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReconciliationGrantsEntitlementsForPendingSubscriptions()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"billing-reconcile-{suffix}@example.com", "Billing Reconcile");

        var checkoutResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/billing/checkout",
            new { planCode = "Business", interval = "Annual" });
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = factory.Services.CreateAsyncScope();
        var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var reconciled = await billingService.ReconcilePendingSubscriptionsAsync();
        reconciled.Should().BeGreaterThan(0);

        var entitlements = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/tenants/{tenantPublicId}/entitlements");
        entitlements.GetProperty("plan").GetString().Should().Be("Business");
    }

    private HttpClient CreateAuthenticatedClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private async Task SendWebhookAsync(string subscriptionId, string eventType)
    {
        var webhookBody = BuildWebhookPayload(
            $"evt_{Guid.NewGuid():N}",
            eventType,
            subscriptionId,
            $"pay_test_{Guid.NewGuid():N}");
        var webhookClient = factory.CreateClient();
        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/razorpay")
        {
            Content = new StringContent(webhookBody, Encoding.UTF8, "application/json")
        };
        webhookRequest.Headers.Add("X-Razorpay-Signature", ComputeWebhookSignature(webhookBody, WebhookSecret));
        var response = await webhookClient.SendAsync(webhookRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string BuildWebhookPayload(
        string eventId,
        string eventType,
        string subscriptionId,
        string paymentId) =>
        $$"""
        {
          "id": "{{eventId}}",
          "event": "{{eventType}}",
          "payload": {
            "subscription": {
              "entity": {
                "id": "{{subscriptionId}}",
                "status": "active"
              }
            },
            "payment": {
              "entity": {
                "id": "{{paymentId}}",
                "status": "captured"
              }
            }
          }
        }
        """;

    private static string ComputeWebhookSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputePaymentSignature(string subscriptionId, string paymentId, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{subscriptionId}|{paymentId}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<Guid> RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass123!",
            displayName = "Billing Test User",
            tenantName
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "SecurePass123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        return me.GetProperty("tenant").GetProperty("publicId").GetGuid();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Api.Authorization;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Privacy.Domain;
using ScopeSeal.Privacy.Services;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class PrivacyEndpointTests(PrivacyPostgresWebApplicationFactory factory)
    : IClassFixture<PrivacyPostgresWebApplicationFactory>
{
    [Fact]
    public async Task CurrentNoticeAndSubprocessors_ArePublic()
    {
        var client = factory.CreateClient();

        var noticeResponse = await client.GetAsync("/api/v1/privacy/notices/current");
        noticeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var notice = await noticeResponse.Content.ReadFromJsonAsync<JsonElement>();
        notice.GetProperty("version").GetString().Should().Be("1.0");

        var subprocessorResponse = await client.GetAsync("/api/v1/privacy/subprocessors");
        subprocessorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var subprocessors = await subprocessorResponse.Content.ReadFromJsonAsync<JsonElement>();
        subprocessors.GetProperty("subprocessors").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RecordConsentsAndWithdrawOptionalMarketing_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"privacy-consent-{suffix}@example.com", "Privacy Consent Tenant");

        var notice = await client.GetFromJsonAsync<JsonElement>("/api/v1/privacy/notices/current");
        var noticePublicId = notice.GetProperty("publicId").GetGuid();

        var recordResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/consents",
            new
            {
                noticePublicId,
                requiredTermsAccepted = true,
                optionalMarketingAccepted = true,
                optionalAnalyticsAccepted = false
            });

        recordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var consents = (await recordResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("consents");
        consents.GetArrayLength().Should().Be(3);

        var marketingConsentPublicId = consents.EnumerateArray()
            .First(c => c.GetProperty("consentType").GetString() == "OptionalMarketing")
            .GetProperty("publicId")
            .GetGuid();

        var withdrawResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/consents/{marketingConsentPublicId}/withdraw",
            new { reason = "No longer interested" });

        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var withdrawn = await withdrawResponse.Content.ReadFromJsonAsync<JsonElement>();
        withdrawn.GetProperty("granted").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ExportRequestCreatesJobAndAdminQueueEntry()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"privacy-export-{suffix}@example.com", "Privacy Export Tenant");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/requests",
            new
            {
                requestType = "Export",
                subject = "Personal data export",
                details = "Please prepare a copy of my account data."
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        request.GetProperty("requestType").GetString().Should().Be("Export");
        request.GetProperty("statusMessage").GetString()
            .Should()
            .Contain("time-limited download link");

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/v1/tenants/{tenantPublicId}/privacy/summary");
        summary.GetProperty("exportJobs").GetArrayLength().Should().Be(1);

        var adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(
            AdminOperatorAuth.OperatorKeyHeader,
            PrivacyPostgresWebApplicationFactory.OperatorApiKey);

        var queueResponse = await adminClient.GetAsync("/api/v1/admin/privacy/queue");
        queueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var queue = await queueResponse.Content.ReadFromJsonAsync<JsonElement>();
        queue.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ErasureRequestSchedulesDeletionWithBackupPurgeMessaging()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"privacy-erasure-{suffix}@example.com", "Privacy Erasure Tenant");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/requests",
            new
            {
                requestType = "Erasure",
                subject = "Account deletion",
                details = "Please delete my account and associated records."
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        request.GetProperty("statusMessage").GetString()
            .Should()
            .Contain("not erased instantly");

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/v1/tenants/{tenantPublicId}/privacy/summary");
        var deletionJobs = summary.GetProperty("deletionJobs");
        deletionJobs.GetArrayLength().Should().Be(1);
        deletionJobs[0].GetProperty("statusMessage").GetString()
            .Should()
            .Contain("does not happen instantly");
    }

    [Fact]
    public async Task PrivacyRequestsAreTenantIsolated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ownerClient = CreateAuthenticatedClient();
        var intruderClient = CreateAuthenticatedClient();

        var ownerTenant = await RegisterAndLoginAsync(ownerClient, $"privacy-owner-{suffix}@example.com", "Owner Tenant");
        await RegisterAndLoginAsync(intruderClient, $"privacy-intruder-{suffix}@example.com", "Intruder Tenant");

        var createResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/tenants/{ownerTenant}/privacy/requests",
            new
            {
                requestType = "Access",
                subject = "Access request",
                details = "Please confirm what data you hold."
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var requestPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId")
            .GetGuid();

        var crossTenantList = await intruderClient.GetAsync($"/api/v1/tenants/{ownerTenant}/privacy/requests");
        crossTenantList.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var crossTenantGet = await intruderClient.GetAsync(
            $"/api/v1/tenants/{ownerTenant}/privacy/requests/{requestPublicId}");
        crossTenantGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PrivacyRightsRemainAvailableAfterDowngrade()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"privacy-downgrade-{suffix}@example.com";
        var tenantPublicId = await RegisterAndLoginAsync(client, email, "Privacy Downgrade Tenant");

        await using var scope = factory.Services.CreateAsyncScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
        var userId = await GetUserIdForEmailAsync(scope, email);
        var tenant = await tenantService.GetCurrentTenantForUserAsync(userId);
        tenant.Should().NotBeNull();

        await entitlementService.AssignPlanAsync(tenant!.TenantId, PlanCode.Pro, EntitlementSource.Trial);
        await entitlementService.AssignPlanAsync(tenant.TenantId, PlanCode.Free, EntitlementSource.FreePlan);

        var exportResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/requests",
            new
            {
                requestType = "Export",
                subject = "Export after downgrade",
                details = "Export should still work on Free plan."
            });

        exportResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ProcessPendingJobsPreparesExportAndAdvancesDeletion()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"privacy-jobs-{suffix}@example.com", "Privacy Jobs Tenant");

        await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/requests",
            new
            {
                requestType = "Export",
                subject = "Export job processing",
                details = "Prepare export."
            });

        await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/privacy/requests",
            new
            {
                requestType = "Erasure",
                subject = "Deletion job processing",
                details = "Start deletion orchestration."
            });

        var adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(
            AdminOperatorAuth.OperatorKeyHeader,
            PrivacyPostgresWebApplicationFactory.OperatorApiKey);

        var processResponse = await adminClient.PostAsync("/api/v1/admin/privacy/jobs/process-pending", null);
        processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processed = await processResponse.Content.ReadFromJsonAsync<JsonElement>();
        processed.GetProperty("processed").GetInt32().Should().BeGreaterThan(0);

        var summary = await client.GetFromJsonAsync<JsonElement>($"/api/v1/tenants/{tenantPublicId}/privacy/summary");
        summary.GetProperty("exportJobs")[0].GetProperty("status").GetString().Should().Be("Ready");
        summary.GetProperty("deletionJobs")[0].GetProperty("currentStep").GetString()
            .Should()
            .NotBe("AccountLock");
    }

    [Fact]
    public async Task RegisterWithoutAgeConfirmation_IsRejected()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"underage-{suffix}@example.com",
            password = "SecurePass1!",
            displayName = "Underage User",
            tenantName = "Underage Tenant",
            confirmedAge18OrAbove = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private HttpClient CreateAuthenticatedClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private static async Task<Guid> GetUserIdForEmailAsync(AsyncServiceScope scope, string email)
    {
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ScopeSeal.Identity.Domain.ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private static async Task<Guid> RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Privacy Test User",
            tenantName,
            confirmedAge18OrAbove = true
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "SecurePass1!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        return me.GetProperty("tenant").GetProperty("publicId").GetGuid();
    }
}

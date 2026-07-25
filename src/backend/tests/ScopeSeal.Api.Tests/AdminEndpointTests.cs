using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.Api.Authorization;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class AdminEndpointTests(PrivacyPostgresWebApplicationFactory factory)
    : IClassFixture<PrivacyPostgresWebApplicationFactory>
{
    [Fact]
    public async Task AdminEndpoints_RequireOperatorKey()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/tenants/search");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchTenants_ReturnsMetadataOnly()
    {
        var userClient = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndLoginAsync(userClient, $"admin-search-{suffix}@example.com", "Admin Search Tenant");

        var adminClient = CreateAdminClient();
        var response = await adminClient.GetAsync("/api/v1/admin/tenants/search?q=Admin%20Search");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        var first = payload.GetProperty("items")[0];
        first.TryGetProperty("documentCount", out _).Should().BeFalse();
        first.GetProperty("currentPlanCode").GetString().Should().Be("Free");
    }

    [Fact]
    public async Task TenantInspection_ReturnsPlanAndCountsWithoutContent()
    {
        var userClient = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndLoginAsync(userClient, $"admin-inspect-{suffix}@example.com", "Inspect Tenant");

        var adminClient = CreateAdminClient();
        var search = await adminClient.GetFromJsonAsync<JsonElement>("/api/v1/admin/tenants/search?q=Inspect%20Tenant");
        var tenantPublicId = search.GetProperty("items")[0].GetProperty("publicId").GetGuid();

        var inspection = await adminClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/admin/tenants/{tenantPublicId}/inspection");

        inspection.GetProperty("name").GetString().Should().Be("Inspect Tenant");
        inspection.GetProperty("currentPlanCode").GetString().Should().Be("Free");
        inspection.TryGetProperty("snapshots", out _).Should().BeFalse();
    }

    [Fact]
    public async Task FeatureFlags_AreSeededAndUpdatable()
    {
        var adminClient = CreateAdminClient();

        var listResponse = await adminClient.GetAsync("/api/v1/admin/feature-flags");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        flags.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(3);

        var updateResponse = await adminClient.PutAsJsonAsync(
            "/api/v1/admin/feature-flags/MaintenanceMode",
            new { isEnabled = true, description = "Maintenance test" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("isEnabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TermsNoticeVersions_CanBeListedAndCreated()
    {
        var adminClient = CreateAdminClient();

        var listResponse = await adminClient.GetAsync("/api/v1/admin/notices/terms");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listed.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/admin/notices/terms",
            new
            {
                version = "1.1",
                title = "Updated draft terms",
                summary = "Operator-updated draft terms summary.",
                effectiveFromUtc = DateTime.UtcNow,
                setAsCurrent = false
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SupportAccessGrant_IsMetadataOnlyAndRevocable()
    {
        var userClient = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndLoginAsync(userClient, $"admin-support-{suffix}@example.com", "Support Tenant");

        var adminClient = CreateAdminClient();
        var search = await adminClient.GetFromJsonAsync<JsonElement>("/api/v1/admin/tenants/search?q=Support%20Tenant");
        var tenantPublicId = search.GetProperty("items")[0].GetProperty("publicId").GetGuid();

        var grantResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/admin/support-access/grants",
            new
            {
                tenantPublicId,
                operatorReference = "operator-123",
                reason = "Billing investigation",
                durationHours = 2
            });

        grantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var grant = await grantResponse.Content.ReadFromJsonAsync<JsonElement>();
        grant.GetProperty("scope").GetString().Should().Be("MetadataOnly");
        var grantPublicId = grant.GetProperty("publicId").GetGuid();

        var revokeResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/support-access/grants/{grantPublicId}/revoke",
            new { reason = "Investigation complete" });

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revoked = await revokeResponse.Content.ReadFromJsonAsync<JsonElement>();
        revoked.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AuditEvents_CanBeListedForTenant()
    {
        var userClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(
            userClient,
            $"admin-audit-{suffix}@example.com",
            "Audit Tenant");

        var workspaceResponse = await userClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name = "Audit Workspace", description = "Audit test", type = "General" });
        workspaceResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var adminClient = CreateAdminClient();
        var response = await adminClient.GetAsync(
            $"/api/v1/admin/audit/events?tenantPublicId={tenantPublicId}&limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PrivacyNoticeVersions_AreListedForOperators()
    {
        var adminClient = CreateAdminClient();
        var response = await adminClient.GetAsync("/api/v1/admin/notices/privacy");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeadLetterSync_ReturnsZeroWhenNoFailedJobs()
    {
        var adminClient = CreateAdminClient();
        var response = await adminClient.PostAsync("/api/v1/admin/jobs/dead-letter/sync", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("added").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    private HttpClient CreateAdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            AdminOperatorAuth.OperatorKeyHeader,
            PrivacyPostgresWebApplicationFactory.OperatorApiKey);
        return client;
    }

    private static async Task<Guid> RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Admin Test User",
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

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        return me.GetProperty("tenant").GetProperty("publicId").GetGuid();
    }
}

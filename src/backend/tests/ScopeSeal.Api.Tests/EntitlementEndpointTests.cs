using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class EntitlementEndpointTests(PostgresWebApplicationFactory factory) : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task NewTenantReceivesFreePlanEntitlements()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndLoginAsync(client, $"entitlements-{suffix}@example.com", "Entitlements Tenant");

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var tenantPublicId = me.GetProperty("tenant").GetProperty("publicId").GetGuid();

        var response = await client.GetAsync($"/api/v1/tenants/{tenantPublicId}/entitlements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("plan").GetString().Should().Be("Free");
        payload.GetProperty("source").GetString().Should().Be("FreePlan");

        var capabilities = payload.GetProperty("capabilities")
            .EnumerateArray()
            .Select(c => c.GetString())
            .ToArray();

        capabilities.Should().Contain("CanAccessPrivacyCentre");
        capabilities.Should().Contain("CanRequestDataExport");
        capabilities.Should().Contain("CanRequestAccountDeletion");
        capabilities.Should().NotContain("CanUseAiExtraction");
    }

    [Fact]
    public async Task UserCannotReadAnotherTenantsEntitlements()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var userAClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var userBClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        await RegisterAndLoginAsync(userAClient, $"ent-a-{suffix}@example.com", "Tenant A");
        await RegisterAndLoginAsync(userBClient, $"ent-b-{suffix}@example.com", "Tenant B");

        var userBTenantPublicId = (await userBClient.GetFromJsonAsync<JsonElement>("/api/v1/auth/me"))
            .GetProperty("tenant")
            .GetProperty("publicId")
            .GetGuid();

        var crossTenantResponse = await userAClient.GetAsync($"/api/v1/tenants/{userBTenantPublicId}/entitlements");
        crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DowngradeBlocksNewUsageWithoutRemovingPrivacyAccess()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"downgrade-{suffix}@example.com";

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        await RegisterAndLoginAsync(client, email, "Downgrade Tenant");

        await using var scope = factory.Services.CreateAsyncScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();

        var userId = await GetUserIdForEmailAsync(scope, email);
        var tenant = await tenantService.GetCurrentTenantForUserAsync(userId);
        tenant.Should().NotBeNull();

        await entitlementService.AssignPlanAsync(
            tenant!.TenantId,
            PlanCode.Pro,
            EntitlementSource.Trial);

        var aiCheck = await entitlementService.CheckCapabilityAsync(
            tenant.TenantId,
            Capability.CanUseAiExtraction);
        aiCheck.IsAllowed.Should().BeTrue();

        await entitlementService.AssignPlanAsync(
            tenant.TenantId,
            PlanCode.Free,
            EntitlementSource.FreePlan);

        aiCheck = await entitlementService.CheckCapabilityAsync(
            tenant.TenantId,
            Capability.CanUseAiExtraction);
        aiCheck.IsAllowed.Should().BeFalse();

        var privacyCheck = await entitlementService.CheckCapabilityAsync(
            tenant.TenantId,
            Capability.CanRequestDataExport);
        privacyCheck.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task SnapshotUsageLimitIsEnforcedOnFreePlan()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"usage-{suffix}@example.com";

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        await RegisterAndLoginAsync(client, email, "Usage Tenant");

        await using var scope = factory.Services.CreateAsyncScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var entitlementService = scope.ServiceProvider.GetRequiredService<IEntitlementService>();

        var userId = await GetUserIdForEmailAsync(scope, email);
        var tenant = await tenantService.GetCurrentTenantForUserAsync(userId);
        tenant.Should().NotBeNull();

        for (var i = 0; i < 5; i++)
        {
            var check = await entitlementService.CheckCapabilityAsync(
                tenant!.TenantId,
                Capability.CanCreateSnapshot);
            check.IsAllowed.Should().BeTrue($"snapshot {i + 1} should be allowed");
            await entitlementService.RecordUsageAsync(
                tenant.TenantId,
                UsageMetric.SnapshotsCreatedThisMonth);
        }

        var blocked = await entitlementService.CheckCapabilityAsync(
            tenant!.TenantId,
            Capability.CanCreateSnapshot);
        blocked.IsAllowed.Should().BeFalse();
    }

    private static async Task<Guid> GetUserIdForEmailAsync(AsyncServiceScope scope, string email)
    {
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private static async Task RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Entitlement Test User",
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
    }
}

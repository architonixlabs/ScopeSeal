using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class AgreementSnapshotEndpointTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly PostgresWebApplicationFactory _factory = factory;

    [Fact]
    public async Task CreateListGetAndUpdateSnapshot_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"snap-{suffix}@example.com", "Snapshot Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Snapshot workspace");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Kitchen agreement", description = "Initial draft" });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var snapshotPublicId = created.GetProperty("publicId").GetGuid();
        created.GetProperty("status").GetString().Should().Be("Draft");
        var updatedAtUtc = created.GetProperty("updatedAtUtc").GetDateTime();

        var listResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetArrayLength().Should().Be(1);

        var getResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshotDetail = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        updatedAtUtc = snapshotDetail.GetProperty("updatedAtUtc").GetDateTime();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}",
            new UpdateAgreementSnapshotRequest(
                "Kitchen agreement v1",
                "Updated draft",
                updatedAtUtc,
                [new SectionItemInput(null, 0, "Cabinet installation", "Upper and lower cabinets")],
                [new SectionItemInput(null, 0, "Appliance purchase", null)],
                [new SectionItemInput(null, 0, "Completed kitchen layout", null)],
                [new SectionItemInput(null, 0, "Site access on weekdays", null)],
                [new PaymentMilestoneInput(null, 0, "Advance", null, 500000L, "INR", null)],
                [new TimelineMilestoneInput(null, 0, "Work start", null, DateTime.UtcNow.AddDays(14))],
                [],
                [new SectionItemInput(null, 0, "Power available on site", null)],
                [new SectionItemInput(null, 0, "Tile selection pending", null)]));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("title").GetString().Should().Be("Kitchen agreement v1");
        updated.GetProperty("scopeItems").GetArrayLength().Should().Be(1);
        updated.GetProperty("paymentMilestones").GetArrayLength().Should().Be(1);
        updated.GetProperty("openQuestions").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsSnapshot()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var userAClient = CreateAuthenticatedClient();
        var userBClient = CreateAuthenticatedClient();

        var tenantA = await RegisterAndLoginAsync(userAClient, $"snap-a-{suffix}@example.com", "Tenant A");
        var tenantB = await RegisterAndLoginAsync(userBClient, $"snap-b-{suffix}@example.com", "Tenant B");

        var workspaceB = await CreateWorkspaceAsync(userBClient, tenantB, "Private workspace");
        var createResponse = await userBClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/snapshots",
            new { title = "Private snapshot" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var snapshotPublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var crossTenantList = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/snapshots");
        crossTenantList.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var crossTenantGet = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspaceB}/snapshots/{snapshotPublicId}");
        crossTenantGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownList = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantA}/workspaces/{workspaceB}/snapshots");
        ownList.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FreePlanBlocksSixthSnapshotCreation()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"snap-limit-{suffix}@example.com", "Limit Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Limit workspace");

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
                new { title = $"Snapshot {i + 1}" });
            response.StatusCode.Should().Be(HttpStatusCode.Created, $"snapshot {i + 1} should succeed");
        }

        var blocked = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Snapshot 6" });
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StaleExpectedUpdatedAtUtc_ReturnsConcurrencyConflict()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"snap-cc-{suffix}@example.com", "Concurrency Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "Concurrency workspace");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = "Concurrency test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var snapshotPublicId = created.GetProperty("publicId").GetGuid();

        var staleTimestamp = created.GetProperty("updatedAtUtc").GetDateTime().AddMinutes(-5);

        var conflictResponse = await client.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}",
            new
            {
                title = "Stale update",
                description = (string?)null,
                expectedUpdatedAtUtc = staleTimestamp,
                scopeItems = Array.Empty<object>(),
                exclusions = Array.Empty<object>(),
                deliverables = Array.Empty<object>(),
                commitments = Array.Empty<object>(),
                paymentMilestones = Array.Empty<object>(),
                timelineMilestones = Array.Empty<object>(),
                dependencies = Array.Empty<object>(),
                assumptions = Array.Empty<object>(),
                openQuestions = Array.Empty<object>()
            });

        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private HttpClient CreateAuthenticatedClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private static async Task<Guid> RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Snapshot Test User",
            tenantName
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

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, Guid tenantPublicId, string name)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name, type = WorkspaceType.General.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspace = await response.Content.ReadFromJsonAsync<JsonElement>();
        return workspace.GetProperty("publicId").GetGuid();
    }
}

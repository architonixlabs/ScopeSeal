using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class WorkspaceEndpointTests(PostgresWebApplicationFactory factory) : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly PostgresWebApplicationFactory _factory = factory;

    [Fact]
    public async Task CreateListAndGetWorkspace_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"ws-{suffix}@example.com", "Workspace Tenant");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new
            {
                name = "Kitchen remodel",
                description = "Scope for kitchen renovation",
                type = WorkspaceType.InteriorDesign.ToString()
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var workspacePublicId = created.GetProperty("publicId").GetGuid();
        created.GetProperty("status").GetString().Should().Be("Draft");

        var listResponse = await client.GetAsync($"/api/v1/tenants/{tenantPublicId}/workspaces");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetArrayLength().Should().Be(1);

        var getResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserCannotAccessAnotherTenantsWorkspace()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var userAClient = CreateAuthenticatedClient();
        var userBClient = CreateAuthenticatedClient();

        var tenantA = await RegisterAndLoginAsync(userAClient, $"ws-a-{suffix}@example.com", "Tenant A");
        var tenantB = await RegisterAndLoginAsync(userBClient, $"ws-b-{suffix}@example.com", "Tenant B");

        var createResponse = await userBClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantB}/workspaces",
            new { name = "Private workspace", type = WorkspaceType.General.ToString() });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspacePublicId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var crossTenantList = await userAClient.GetAsync($"/api/v1/tenants/{tenantB}/workspaces");
        crossTenantList.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var crossTenantGet = await userAClient.GetAsync(
            $"/api/v1/tenants/{tenantB}/workspaces/{workspacePublicId}");
        crossTenantGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownList = await userAClient.GetAsync($"/api/v1/tenants/{tenantA}/workspaces");
        ownList.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FreePlanBlocksFourthWorkspaceCreation()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"limit-{suffix}@example.com", "Limit Tenant");

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/v1/tenants/{tenantPublicId}/workspaces",
                new { name = $"Workspace {i + 1}", type = WorkspaceType.General.ToString() });
            response.StatusCode.Should().Be(HttpStatusCode.Created, $"workspace {i + 1} should succeed");
        }

        var blocked = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name = "Workspace 4", type = WorkspaceType.General.ToString() });
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ArchiveWorkspaceDecrementsUsageAllowingNewCreation()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"arch-{suffix}@example.com", "Archive Tenant");

        Guid? firstWorkspaceId = null;
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/v1/tenants/{tenantPublicId}/workspaces",
                new { name = $"Workspace {i + 1}", type = WorkspaceType.General.ToString() });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            if (i == 0)
            {
                firstWorkspaceId = (await response.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("publicId").GetGuid();
            }
        }

        var blocked = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name = "Workspace 4", type = WorkspaceType.General.ToString() });
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var archiveResponse = await client.PostAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{firstWorkspaceId}/archive",
            null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retry = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name = "Workspace replacement", type = WorkspaceType.General.ToString() });
        retry.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DashboardReturnsWorkspaceSummary()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"dash-{suffix}@example.com", "Dashboard Tenant");

        await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name = "Dashboard workspace", type = WorkspaceType.General.ToString() });

        var dashboardResponse = await client.GetAsync($"/api/v1/tenants/{tenantPublicId}/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<JsonElement>();
        dashboard.GetProperty("totalWorkspaces").GetInt32().Should().Be(1);
        dashboard.GetProperty("activeWorkspaces").GetInt32().Should().Be(1);
        dashboard.GetProperty("activeWorkspaceLimit").GetInt64().Should().Be(3);
    }

    [Fact]
    public async Task PartyAndContactWorkflow_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"party-{suffix}@example.com", "Party Tenant");

        var contactResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/contacts",
            new { displayName = "Alex Client", email = "alex@example.com" });
        contactResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var contactPublicId = (await contactResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var partyResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/parties",
            new { displayName = "Alex Client", roleLabel = "Homeowner", contactPublicId });
        partyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var partyPublicId = (await partyResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var workspaceResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name = "Party workspace", type = WorkspaceType.InteriorDesign.ToString() });
        workspaceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var workspacePublicId = (await workspaceResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("publicId").GetGuid();

        var linkResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/parties",
            new { partyPublicId, role = WorkspacePartyRole.Client.ToString() });
        linkResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var templatesResponse = await client.GetAsync($"/api/v1/tenants/{tenantPublicId}/templates");
        templatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var templates = await templatesResponse.Content.ReadFromJsonAsync<JsonElement>();
        templates.GetArrayLength().Should().BeGreaterThan(0);
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
            displayName = "Workspace Test User",
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

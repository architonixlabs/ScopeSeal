using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Api.Tests;

[Collection("SecurityIntegration")]
public sealed class SecurityHardeningTests(SecurityPostgresWebApplicationFactory factory)
    : IClassFixture<SecurityPostgresWebApplicationFactory>
{
    private const string XssPayload = "<script>alert('xss')</script>";

    [Fact]
    public async Task ApiResponsesIncludeSecurityHeaders()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("Permissions-Policy");
    }

    [Fact]
    public async Task RandomResourceIdReturnsNotFound()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"idor-{suffix}@example.com", "IDOR Tenant");
        var randomWorkspaceId = Guid.NewGuid();
        var randomSnapshotId = Guid.NewGuid();

        var workspaceResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{randomWorkspaceId}");
        workspaceResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var snapshotResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{randomWorkspaceId}/snapshots/{randomSnapshotId}");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var documentResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/documents/download?token={Guid.NewGuid()}");
        documentResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrossTenantWorkspaceEnumerationIsBlocked()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ownerClient = CreateAuthenticatedClient();
        var intruderClient = CreateAuthenticatedClient();

        var ownerTenant = await RegisterAndLoginAsync(ownerClient, $"sec-owner-{suffix}@example.com", "Owner");
        await RegisterAndLoginAsync(intruderClient, $"sec-intruder-{suffix}@example.com", "Intruder");
        var ownerWorkspace = await CreateWorkspaceAsync(ownerClient, ownerTenant, "Owner workspace");

        var response = await intruderClient.GetAsync(
            $"/api/v1/tenants/{ownerTenant}/workspaces/{ownerWorkspace}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SnapshotXssPayloadIsStoredAndReturnedAsJsonEncodedText()
    {
        var client = CreateAuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantPublicId = await RegisterAndLoginAsync(client, $"xss-{suffix}@example.com", "XSS Tenant");
        var workspacePublicId = await CreateWorkspaceAsync(client, tenantPublicId, "XSS workspace");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots",
            new { title = XssPayload, description = "XSS fixture" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var snapshotPublicId = created.GetProperty("publicId").GetGuid();
        created.GetProperty("title").GetString().Should().Be(XssPayload);

        var getResponse = await client.GetAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        getBody.Should().Contain("\\u003Cscript\\u003E");
        getBody.Should().NotContain("<script>alert");
    }

    private HttpClient CreateAuthenticatedClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
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
            displayName = "Security Test User",
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

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, Guid tenantPublicId, string name)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantPublicId}/workspaces",
            new { name, type = WorkspaceType.General.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicId").GetGuid();
    }
}

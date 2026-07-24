using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class TenantIsolationTests(PostgresWebApplicationFactory factory) : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task UserCannotAccessAnotherUsersTenant()
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

        await RegisterAndLoginAsync(userAClient, $"a-{suffix}@example.com", "Tenant A");
        await RegisterAndLoginAsync(userBClient, $"b-{suffix}@example.com", "Tenant B");

        var userAMe = await userAClient.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var userBPublicId = (await userBClient.GetFromJsonAsync<JsonElement>("/api/v1/auth/me"))
            .GetProperty("tenant")
            .GetProperty("publicId")
            .GetGuid();

        var crossTenantResponse = await userAClient.GetAsync($"/api/v1/tenants/{userBPublicId}");
        crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownTenantPublicId = userAMe.GetProperty("tenant").GetProperty("publicId").GetGuid();
        var ownTenantResponse = await userAClient.GetAsync($"/api/v1/tenants/{ownTenantPublicId}");
        ownTenantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task RegisterAndLoginAsync(HttpClient client, string email, string tenantName)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Isolation Test User",
            tenantName
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

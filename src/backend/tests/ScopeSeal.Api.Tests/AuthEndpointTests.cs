using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class AuthEndpointTests(PostgresWebApplicationFactory factory) : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task RegisterLoginAndGetMe_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"user-{suffix}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "SecurePass1!",
            displayName = "Test User",
            tenantName = $"Tenant {suffix}"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "SecurePass1!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await meResponse.Content.ReadAsStringAsync();
        body.Should().Contain(email);
        body.Should().Contain("Tenant");
    }

    [Fact]
    public async Task GetMe_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

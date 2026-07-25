using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScopeSeal.Api.Tests;

[Collection("PostgresIntegration")]
public sealed class SystemEndpointTests(PostgresWebApplicationFactory factory) : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetSystemStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/system/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ScopeSeal.Api");
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

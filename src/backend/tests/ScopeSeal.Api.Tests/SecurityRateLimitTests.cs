using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScopeSeal.Api.Tests;

[Collection("SecurityRateLimit")]
public sealed class SecurityRateLimitTests(SecurityRateLimitWebApplicationFactory factory)
    : IClassFixture<SecurityRateLimitWebApplicationFactory>
{
    [Fact]
    public async Task LoginRateLimitReturns429()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        HttpStatusCode? lastStatus = null;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = "nonexistent@example.com",
                password = "WrongPass1!"
            });
            lastStatus = response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        lastStatus.Should().Be(HttpStatusCode.TooManyRequests);
    }
}

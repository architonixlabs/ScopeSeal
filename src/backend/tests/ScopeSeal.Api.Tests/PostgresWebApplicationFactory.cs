using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ScopeSeal.Api.Tests;

public class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var environmentConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (!string.IsNullOrWhiteSpace(environmentConnection))
        {
            ConnectionString = environmentConnection;
            return;
        }

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("scopeseal_test")
            .WithUsername("scopeseal")
            .WithPassword("scopeseal_test")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public new async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("ScopeSeal:Auth:JwtSecret", "test-secret-minimum-32-characters-long");
        builder.UseSetting("ScopeSeal:Auth:RequireEmailVerification", "false");
        builder.UseSetting("ScopeSeal:Security:RateLimit:AuthPermitLimit", "100000");
    }
}

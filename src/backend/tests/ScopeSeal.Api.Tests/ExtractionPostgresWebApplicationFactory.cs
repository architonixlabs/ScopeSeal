using Microsoft.AspNetCore.Hosting;

namespace ScopeSeal.Api.Tests;

public sealed class ExtractionPostgresWebApplicationFactory : PostgresWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ScopeSeal:Ai:Mode", "LocalProcessing");
        builder.UseSetting("ScopeSeal:Ai:KillSwitchEnabled", "false");
    }
}

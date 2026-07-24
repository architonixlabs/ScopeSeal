using Microsoft.AspNetCore.Hosting;

namespace ScopeSeal.Api.Tests;

public sealed class ChangeLedgerPostgresWebApplicationFactory : PostgresWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Plans:Free:MaxExternalReviewers", "5");
    }
}

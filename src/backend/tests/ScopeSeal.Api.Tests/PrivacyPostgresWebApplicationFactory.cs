using Microsoft.AspNetCore.Hosting;

namespace ScopeSeal.Api.Tests;

public sealed class PrivacyPostgresWebApplicationFactory : PostgresWebApplicationFactory
{
    public const string OperatorApiKey = "test-operator-key-loop11";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ScopeSeal:Privacy:OperatorApiKey", OperatorApiKey);
        builder.UseSetting("ScopeSeal:Privacy:ExportLinkExpiryDays", "7");
        builder.UseSetting("ScopeSeal:Privacy:BackupPurgeGraceDays", "30");
    }
}

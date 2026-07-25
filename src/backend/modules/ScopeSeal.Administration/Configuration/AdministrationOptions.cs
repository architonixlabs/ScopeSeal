namespace ScopeSeal.Administration.Configuration;

public sealed class AdministrationOptions
{
    public const string SectionName = "ScopeSeal:Administration";

    public string OperatorApiKey { get; set; } = string.Empty;

    public int DefaultSupportAccessHours { get; set; } = 4;

    public int TenantSearchMaxResults { get; set; } = 25;
}

using System.ComponentModel.DataAnnotations;

namespace ScopeSeal.Privacy.Configuration;

public sealed class PrivacyOptions
{
    public const string SectionName = "ScopeSeal:Privacy";

    [Range(7, 365)]
    public int ExportLinkExpiryDays { get; set; } = 7;

    [Range(7, 180)]
    public int BackupPurgeGraceDays { get; set; } = 30;

    public string OperatorApiKey { get; set; } = string.Empty;
}

namespace ScopeSeal.Privacy.Domain;

public sealed class DataExportJob
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public Guid PrivacyRequestId { get; set; }

    public ExportJobStatus Status { get; set; }

    public string? DownloadToken { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

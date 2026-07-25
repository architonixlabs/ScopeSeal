namespace ScopeSeal.Administration.Domain;

public sealed class SupportAccessGrant
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public string OperatorReference { get; set; } = string.Empty;

    public SupportAccessScope Scope { get; set; } = SupportAccessScope.MetadataOnly;

    public string Reason { get; set; } = string.Empty;

    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
}

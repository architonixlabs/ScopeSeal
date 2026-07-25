namespace ScopeSeal.Privacy.Domain;

public sealed class SubprocessorEntry
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string DataProcessed { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string ContractStatus { get; set; } = string.Empty;

    public string DpaStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }
}

namespace ScopeSeal.Extraction.Domain;

public sealed class ExtractedFact
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid ExtractionRunId { get; set; }

    public ExtractionRun ExtractionRun { get; set; } = null!;

    public ExtractedFactSectionType SectionType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public long? AmountMinorUnits { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal ConfidenceScore { get; set; }

    public FactReviewStatus ReviewStatus { get; set; } = FactReviewStatus.Draft;

    public string SourceDocumentName { get; set; } = string.Empty;

    public string SourceHashValue { get; set; } = string.Empty;

    public int? SourcePageNumber { get; set; }

    public string? SourceExcerpt { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAtUtc { get; set; }

    public Guid? ReviewedByUserId { get; set; }
}

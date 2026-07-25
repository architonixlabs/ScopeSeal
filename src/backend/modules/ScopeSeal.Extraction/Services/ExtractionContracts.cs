using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Extraction.Domain;

namespace ScopeSeal.Extraction.Services;

public sealed record ExtractionFactSource(
    int? PageNumber,
    string? Excerpt);

public sealed record ProviderExtractedFact(
    ExtractedFactSectionType SectionType,
    string Title,
    string? Description,
    long? AmountMinorUnits,
    string? CurrencyCode,
    decimal ConfidenceScore,
    ExtractionFactSource Source);

public sealed record ExtractionProviderResult(
    IReadOnlyList<ProviderExtractedFact> Facts,
    string? ProviderName);

public sealed record ExtractionRunSummary(
    Guid PublicId,
    Guid DocumentPublicId,
    Guid? SnapshotPublicId,
    ExtractionRunStatus Status,
    string AiMode,
    int FactCount,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage);

public sealed record ExtractedFactDetail(
    Guid PublicId,
    ExtractedFactSectionType SectionType,
    string Title,
    string? Description,
    long? AmountMinorUnits,
    string? CurrencyCode,
    decimal ConfidenceScore,
    FactReviewStatus ReviewStatus,
    string SourceDocumentName,
    string SourceHashValue,
    int? SourcePageNumber,
    string? SourceExcerpt,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record ExtractionRunDetail(
    Guid PublicId,
    Guid DocumentPublicId,
    Guid? SnapshotPublicId,
    ExtractionRunStatus Status,
    string AiMode,
    IReadOnlyList<ExtractedFactDetail> Facts,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage);

public sealed record CreateExtractionRunRequest(Guid? SnapshotPublicId);

public sealed record ReviewExtractedFactRequest(FactReviewStatus ReviewStatus);

public sealed record ApplyExtractionResult(
    ExtractionRunDetail Run,
    AgreementSnapshotDetail? Snapshot);

public interface IAiExtractionProvider
{
    string Mode { get; }

    Task<ExtractionProviderResult> ExtractAsync(
        ExtractionProviderContext context,
        CancellationToken cancellationToken = default);
}

public sealed record ExtractionProviderContext(
    Guid TenantId,
    string DocumentFileName,
    string ContentType,
    string SourceHashValue,
    Stream DocumentContent);

public interface IExtractionSchemaValidator
{
    (bool IsValid, string? Error) ValidateFacts(IReadOnlyList<ProviderExtractedFact> facts);
}

public interface IExtractionService
{
    Task<(ExtractionRunDetail? Run, string? Error)> CreateExtractionRunAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        Guid userId,
        CreateExtractionRunRequest request,
        CancellationToken cancellationToken = default);

    Task<ExtractionRunDetail?> GetExtractionRunAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        CancellationToken cancellationToken = default);

    Task<(ExtractedFactDetail? Fact, string? Error)> ReviewFactAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        Guid factPublicId,
        Guid userId,
        ReviewExtractedFactRequest request,
        CancellationToken cancellationToken = default);

    Task<(ApplyExtractionResult? Result, string? Error)> ApplyAcceptedFactsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        Guid snapshotPublicId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IProcessingJobProcessor
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}

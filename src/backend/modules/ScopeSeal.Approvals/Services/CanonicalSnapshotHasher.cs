using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.AgreementSnapshots.Services;

namespace ScopeSeal.Approvals.Services;

public static class CanonicalSnapshotHasher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string ComputeSha256Hex(AgreementSnapshotDetail snapshot)
    {
        var canonical = new CanonicalSnapshotPayload(
            snapshot.PublicId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status.ToString(),
            snapshot.VersionNumber,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc,
            MapSections(snapshot.ScopeItems),
            MapSections(snapshot.Exclusions),
            MapSections(snapshot.Deliverables),
            MapSections(snapshot.Commitments),
            snapshot.PaymentMilestones
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.PublicId)
                .Select(m => new CanonicalPaymentMilestone(
                    m.PublicId,
                    m.SortOrder,
                    m.Title,
                    m.Description,
                    m.AmountMinorUnits,
                    m.CurrencyCode,
                    m.DueDateUtc))
                .ToList(),
            snapshot.TimelineMilestones
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.PublicId)
                .Select(m => new CanonicalTimelineMilestone(
                    m.PublicId,
                    m.SortOrder,
                    m.Title,
                    m.Description,
                    m.TargetDateUtc))
                .ToList(),
            MapSections(snapshot.Dependencies),
            MapSections(snapshot.Assumptions),
            MapSections(snapshot.OpenQuestions));

        var json = JsonSerializer.Serialize(canonical, SerializerOptions);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeSha256Hex(AgreementSnapshot snapshot)
    {
        var detail = new AgreementSnapshotDetail(
            snapshot.PublicId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.VersionNumber,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc,
            snapshot.ScopeItems
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Exclusions
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Deliverables
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Commitments
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.PaymentMilestones
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new PaymentMilestoneDetail(
                    i.PublicId,
                    i.SortOrder,
                    i.Title,
                    i.Description,
                    i.AmountMinorUnits,
                    i.CurrencyCode,
                    i.DueDateUtc))
                .ToList(),
            snapshot.TimelineMilestones
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new TimelineMilestoneDetail(
                    i.PublicId,
                    i.SortOrder,
                    i.Title,
                    i.Description,
                    i.TargetDateUtc))
                .ToList(),
            snapshot.Dependencies
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Assumptions
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.OpenQuestions
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.PublicId)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList());

        return ComputeSha256Hex(detail);
    }

    private static List<CanonicalSectionItem> MapSections(IReadOnlyList<SectionItemDetail> items) =>
        items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.PublicId)
            .Select(i => new CanonicalSectionItem(i.PublicId, i.SortOrder, i.Title, i.Description))
            .ToList();

    private sealed record CanonicalSnapshotPayload(
        Guid PublicId,
        string Title,
        string? Description,
        string Status,
        int VersionNumber,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        IReadOnlyList<CanonicalSectionItem> ScopeItems,
        IReadOnlyList<CanonicalSectionItem> Exclusions,
        IReadOnlyList<CanonicalSectionItem> Deliverables,
        IReadOnlyList<CanonicalSectionItem> Commitments,
        IReadOnlyList<CanonicalPaymentMilestone> PaymentMilestones,
        IReadOnlyList<CanonicalTimelineMilestone> TimelineMilestones,
        IReadOnlyList<CanonicalSectionItem> Dependencies,
        IReadOnlyList<CanonicalSectionItem> Assumptions,
        IReadOnlyList<CanonicalSectionItem> OpenQuestions);

    private sealed record CanonicalSectionItem(
        Guid PublicId,
        int SortOrder,
        string Title,
        string? Description);

    private sealed record CanonicalPaymentMilestone(
        Guid PublicId,
        int SortOrder,
        string Title,
        string? Description,
        long? AmountMinorUnits,
        string? CurrencyCode,
        DateTime? DueDateUtc);

    private sealed record CanonicalTimelineMilestone(
        Guid PublicId,
        int SortOrder,
        string Title,
        string? Description,
        DateTime? TargetDateUtc);
}

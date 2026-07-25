using ScopeSeal.AgreementSnapshots.Services;

namespace ScopeSeal.ChangeLedger.Services;

public static class SnapshotDiffService
{
    public static SnapshotDiffDetail ComputeDiff(
        AgreementSnapshotDetail from,
        AgreementSnapshotDetail to)
    {
        var changes = new List<SectionDiffEntry>();

        if (!string.Equals(from.Title, to.Title, StringComparison.Ordinal))
        {
            changes.Add(new SectionDiffEntry("Title", "Modified", null, to.Title, from.Title, null, null));
        }

        if (!string.Equals(from.Description, to.Description, StringComparison.Ordinal))
        {
            changes.Add(new SectionDiffEntry("Description", "Modified", null, to.Description, from.Description, null, null));
        }

        DiffSectionItems(changes, "ScopeItems", from.ScopeItems, to.ScopeItems);
        DiffSectionItems(changes, "Exclusions", from.Exclusions, to.Exclusions);
        DiffSectionItems(changes, "Deliverables", from.Deliverables, to.Deliverables);
        DiffSectionItems(changes, "Commitments", from.Commitments, to.Commitments);
        DiffSectionItems(changes, "Dependencies", from.Dependencies, to.Dependencies);
        DiffSectionItems(changes, "Assumptions", from.Assumptions, to.Assumptions);
        DiffSectionItems(changes, "OpenQuestions", from.OpenQuestions, to.OpenQuestions);
        DiffPaymentMilestones(changes, from.PaymentMilestones, to.PaymentMilestones);
        DiffTimelineMilestones(changes, from.TimelineMilestones, to.TimelineMilestones);

        return new SnapshotDiffDetail(
            from.PublicId,
            from.VersionNumber,
            to.PublicId,
            to.VersionNumber,
            changes);
    }

    private static void DiffSectionItems(
        List<SectionDiffEntry> changes,
        string sectionName,
        IReadOnlyList<SectionItemDetail> fromItems,
        IReadOnlyList<SectionItemDetail> toItems)
    {
        var fromById = fromItems.ToDictionary(i => i.PublicId);
        var toById = toItems.ToDictionary(i => i.PublicId);

        foreach (var item in toItems)
        {
            if (!fromById.TryGetValue(item.PublicId, out var previous))
            {
                changes.Add(new SectionDiffEntry(
                    sectionName, "Added", item.PublicId, item.Title, null, item.Description, null));
            }
            else if (!string.Equals(previous.Title, item.Title, StringComparison.Ordinal) ||
                     !string.Equals(previous.Description, item.Description, StringComparison.Ordinal))
            {
                changes.Add(new SectionDiffEntry(
                    sectionName, "Modified", item.PublicId, item.Title, previous.Title, item.Description, previous.Description));
            }
        }

        foreach (var item in fromItems)
        {
            if (!toById.ContainsKey(item.PublicId))
            {
                changes.Add(new SectionDiffEntry(
                    sectionName, "Removed", item.PublicId, null, item.Title, null, item.Description));
            }
        }
    }

    private static void DiffPaymentMilestones(
        List<SectionDiffEntry> changes,
        IReadOnlyList<PaymentMilestoneDetail> fromItems,
        IReadOnlyList<PaymentMilestoneDetail> toItems)
    {
        var fromById = fromItems.ToDictionary(i => i.PublicId);
        var toById = toItems.ToDictionary(i => i.PublicId);

        foreach (var item in toItems)
        {
            if (!fromById.TryGetValue(item.PublicId, out var previous))
            {
                changes.Add(new SectionDiffEntry(
                    "PaymentMilestones", "Added", item.PublicId, item.Title, null, item.Description, null));
            }
            else if (previous.Title != item.Title ||
                     previous.Description != item.Description ||
                     previous.AmountMinorUnits != item.AmountMinorUnits ||
                     previous.CurrencyCode != item.CurrencyCode ||
                     previous.DueDateUtc != item.DueDateUtc)
            {
                changes.Add(new SectionDiffEntry(
                    "PaymentMilestones", "Modified", item.PublicId, item.Title, previous.Title, item.Description, previous.Description));
            }
        }

        foreach (var item in fromItems)
        {
            if (!toById.ContainsKey(item.PublicId))
            {
                changes.Add(new SectionDiffEntry(
                    "PaymentMilestones", "Removed", item.PublicId, null, item.Title, null, item.Description));
            }
        }
    }

    private static void DiffTimelineMilestones(
        List<SectionDiffEntry> changes,
        IReadOnlyList<TimelineMilestoneDetail> fromItems,
        IReadOnlyList<TimelineMilestoneDetail> toItems)
    {
        var fromById = fromItems.ToDictionary(i => i.PublicId);
        var toById = toItems.ToDictionary(i => i.PublicId);

        foreach (var item in toItems)
        {
            if (!fromById.TryGetValue(item.PublicId, out var previous))
            {
                changes.Add(new SectionDiffEntry(
                    "TimelineMilestones", "Added", item.PublicId, item.Title, null, item.Description, null));
            }
            else if (previous.Title != item.Title ||
                     previous.Description != item.Description ||
                     previous.TargetDateUtc != item.TargetDateUtc)
            {
                changes.Add(new SectionDiffEntry(
                    "TimelineMilestones", "Modified", item.PublicId, item.Title, previous.Title, item.Description, previous.Description));
            }
        }

        foreach (var item in fromItems)
        {
            if (!toById.ContainsKey(item.PublicId))
            {
                changes.Add(new SectionDiffEntry(
                    "TimelineMilestones", "Removed", item.PublicId, null, item.Title, null, item.Description));
            }
        }
    }
}

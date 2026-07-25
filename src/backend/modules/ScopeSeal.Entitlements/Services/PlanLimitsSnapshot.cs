using System.Text.Json;
using ScopeSeal.Entitlements.Configuration;
using ScopeSeal.Entitlements.Domain;

namespace ScopeSeal.Entitlements.Services;

public sealed record PlanLimitsSnapshot
{
    public int MaxMembers { get; init; }

    public int MaxActiveWorkspaces { get; init; }

    public int MaxSnapshotsPerMonth { get; init; }

    public long MaxStorageBytes { get; init; }

    public int MaxAiExtractionsPerMonth { get; init; }

    public int MaxExternalReviewers { get; init; }

    public int MaxExportDownloadsPerMonth { get; init; }

    public IReadOnlySet<Capability> EnabledCapabilities { get; init; } = new HashSet<Capability>();

    public static PlanLimitsSnapshot FromDefinition(PlanDefinitionOptions definition)
    {
        var capabilities = new HashSet<Capability>
        {
            Capability.CanCreateWorkspace,
            Capability.CanCreateSnapshot,
            Capability.CanUploadDocument,
            Capability.CanInviteExternalReviewer,
            Capability.CanAccessPrivacyCentre,
            Capability.CanRequestDataExport,
            Capability.CanRequestAccountDeletion
        };

        if (definition.CanUseAiExtraction)
        {
            capabilities.Add(Capability.CanUseAiExtraction);
        }

        if (definition.CanUseOcr)
        {
            capabilities.Add(Capability.CanUseOcr);
        }

        if (definition.CanTranscribeAudio)
        {
            capabilities.Add(Capability.CanTranscribeAudio);
        }

        if (definition.CanUseChangeRequestWorkflow)
        {
            capabilities.Add(Capability.CanUseChangeRequestWorkflow);
        }

        if (definition.CanExportAdvancedPdf)
        {
            capabilities.Add(Capability.CanExportAdvancedPdf);
        }

        if (definition.CanUseCustomLogo)
        {
            capabilities.Add(Capability.CanUseCustomLogo);
        }

        if (definition.CanManageTeamMembers)
        {
            capabilities.Add(Capability.CanManageTeamMembers);
        }

        if (definition.CanUseSharedTemplates)
        {
            capabilities.Add(Capability.CanUseSharedTemplates);
        }

        if (definition.CanConfigureRetention)
        {
            capabilities.Add(Capability.CanConfigureRetention);
        }

        if (definition.CanAccessApi)
        {
            capabilities.Add(Capability.CanAccessApi);
        }

        return new PlanLimitsSnapshot
        {
            MaxMembers = definition.MaxMembers,
            MaxActiveWorkspaces = definition.MaxActiveWorkspaces,
            MaxSnapshotsPerMonth = definition.MaxSnapshotsPerMonth,
            MaxStorageBytes = definition.MaxStorageBytes,
            MaxAiExtractionsPerMonth = definition.MaxAiExtractionsPerMonth,
            MaxExternalReviewers = definition.MaxExternalReviewers,
            MaxExportDownloadsPerMonth = definition.MaxExportDownloadsPerMonth,
            EnabledCapabilities = capabilities
        };
    }

    public static PlanLimitsSnapshot FromJson(string json) =>
        JsonSerializer.Deserialize<PlanLimitsSnapshotDto>(json)?.ToSnapshot()
        ?? throw new InvalidOperationException("Plan limits snapshot is invalid.");

    public string ToJson() => JsonSerializer.Serialize(PlanLimitsSnapshotDto.FromSnapshot(this));

    private sealed class PlanLimitsSnapshotDto
    {
        public int MaxMembers { get; init; }

        public int MaxActiveWorkspaces { get; init; }

        public int MaxSnapshotsPerMonth { get; init; }

        public long MaxStorageBytes { get; init; }

        public int MaxAiExtractionsPerMonth { get; init; }

        public int MaxExternalReviewers { get; init; }

        public int MaxExportDownloadsPerMonth { get; init; }

        public Capability[] EnabledCapabilities { get; init; } = [];

        public PlanLimitsSnapshot ToSnapshot() => new()
        {
            MaxMembers = MaxMembers,
            MaxActiveWorkspaces = MaxActiveWorkspaces,
            MaxSnapshotsPerMonth = MaxSnapshotsPerMonth,
            MaxStorageBytes = MaxStorageBytes,
            MaxAiExtractionsPerMonth = MaxAiExtractionsPerMonth,
            MaxExternalReviewers = MaxExternalReviewers,
            MaxExportDownloadsPerMonth = MaxExportDownloadsPerMonth,
            EnabledCapabilities = EnabledCapabilities.ToHashSet()
        };

        public static PlanLimitsSnapshotDto FromSnapshot(PlanLimitsSnapshot snapshot) => new()
        {
            MaxMembers = snapshot.MaxMembers,
            MaxActiveWorkspaces = snapshot.MaxActiveWorkspaces,
            MaxSnapshotsPerMonth = snapshot.MaxSnapshotsPerMonth,
            MaxStorageBytes = snapshot.MaxStorageBytes,
            MaxAiExtractionsPerMonth = snapshot.MaxAiExtractionsPerMonth,
            MaxExternalReviewers = snapshot.MaxExternalReviewers,
            MaxExportDownloadsPerMonth = snapshot.MaxExportDownloadsPerMonth,
            EnabledCapabilities = snapshot.EnabledCapabilities.OrderBy(c => c).ToArray()
        };
    }
}

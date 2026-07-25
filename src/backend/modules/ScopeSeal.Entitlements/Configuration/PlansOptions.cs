using System.ComponentModel.DataAnnotations;
using ScopeSeal.Entitlements.Domain;

namespace ScopeSeal.Entitlements.Configuration;

public sealed class PlansOptions
{
    public const string SectionName = "Plans";

    [Required]
    public PlanDefinitionOptions Free { get; init; } = new();

    [Required]
    public PlanDefinitionOptions Pro { get; init; } = new();

    [Required]
    public PlanDefinitionOptions Business { get; init; } = new();

    public PlanDefinitionOptions GetDefinition(PlanCode planCode) => planCode switch
    {
        PlanCode.Free => Free,
        PlanCode.Pro => Pro,
        PlanCode.Business => Business,
        _ => throw new ArgumentOutOfRangeException(nameof(planCode), planCode, "Unknown plan code.")
    };
}

public sealed class PlanDefinitionOptions
{
    [Range(1, int.MaxValue)]
    public int Version { get; init; } = 1;

    [Range(1, 1000)]
    public int MaxMembers { get; init; } = 1;

    [Range(0, int.MaxValue)]
    public int MaxActiveWorkspaces { get; init; } = 3;

    [Range(0, int.MaxValue)]
    public int MaxSnapshotsPerMonth { get; init; } = 5;

    [Range(0, long.MaxValue)]
    public long MaxStorageBytes { get; init; } = 104_857_600;

    [Range(0, int.MaxValue)]
    public int MaxAiExtractionsPerMonth { get; init; } = 2;

    [Range(0, int.MaxValue)]
    public int MaxExternalReviewers { get; init; } = 1;

    [Range(0, int.MaxValue)]
    public int MaxExportDownloadsPerMonth { get; init; } = 10;

    public bool CanUseAiExtraction { get; init; }

    public bool CanUseOcr { get; init; }

    public bool CanTranscribeAudio { get; init; }

    public bool CanUseChangeRequestWorkflow { get; init; }

    public bool CanExportAdvancedPdf { get; init; }

    public bool CanUseCustomLogo { get; init; }

    public bool CanManageTeamMembers { get; init; }

    public bool CanUseSharedTemplates { get; init; }

    public bool CanConfigureRetention { get; init; }

    public bool CanAccessApi { get; init; }
}

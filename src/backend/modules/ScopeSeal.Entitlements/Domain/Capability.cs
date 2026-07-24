namespace ScopeSeal.Entitlements.Domain;

public enum Capability
{
    CanCreateWorkspace = 0,
    CanCreateSnapshot = 1,
    CanUploadDocument = 2,
    CanUseAiExtraction = 3,
    CanUseOcr = 4,
    CanTranscribeAudio = 5,
    CanInviteExternalReviewer = 6,
    CanUseChangeRequestWorkflow = 7,
    CanExportAdvancedPdf = 8,
    CanUseCustomLogo = 9,
    CanManageTeamMembers = 10,
    CanUseSharedTemplates = 11,
    CanConfigureRetention = 12,
    CanAccessApi = 13,
    CanAccessPrivacyCentre = 14,
    CanRequestDataExport = 15,
    CanRequestAccountDeletion = 16
}

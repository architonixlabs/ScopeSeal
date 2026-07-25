using ScopeSeal.Extraction.Services;

namespace ScopeSeal.Infrastructure.Services.Providers;

public sealed class ApprovedExternalProviderExtractionProvider : IAiExtractionProvider
{
    public string Mode => "ApprovedExternalProvider";

    public Task<ExtractionProviderResult> ExtractAsync(
        ExtractionProviderContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Approved external AI provider is not configured. Configure a provider adapter before enabling this mode.");
}

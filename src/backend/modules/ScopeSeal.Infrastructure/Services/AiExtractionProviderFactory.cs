using Microsoft.Extensions.Options;
using ScopeSeal.Extraction.Services;
using ScopeSeal.Infrastructure.Services.Providers;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services;

public sealed class AiExtractionProviderFactory(
    IOptions<ScopeSealOptions> scopeSealOptions,
    ManualOnlyExtractionProvider manualOnlyProvider,
    LocalProcessingExtractionProvider localProcessingProvider,
    ApprovedExternalProviderExtractionProvider externalProvider)
{
    public IAiExtractionProvider Resolve()
    {
        var mode = scopeSealOptions.Value.Ai.Mode;
        return mode switch
        {
            "LocalProcessing" => localProcessingProvider,
            "ApprovedExternalProvider" => externalProvider,
            _ => manualOnlyProvider
        };
    }

    public string CurrentMode => scopeSealOptions.Value.Ai.Mode;
}

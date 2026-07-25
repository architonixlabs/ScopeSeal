using ScopeSeal.Extraction.Services;

namespace ScopeSeal.Infrastructure.Services.Providers;

public sealed class ManualOnlyExtractionProvider : IAiExtractionProvider
{
    public string Mode => "ManualOnly";

    public Task<ExtractionProviderResult> ExtractAsync(
        ExtractionProviderContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ExtractionProviderResult([], "ManualOnly"));
}

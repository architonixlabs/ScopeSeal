using ScopeSeal.Extraction.Services;

namespace ScopeSeal.Worker;

public sealed class ProcessingJobWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessingJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScopeSeal processing worker started at {Utc}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IProcessingJobProcessor>();
                var processed = await processor.ProcessPendingAsync(stoppingToken);
                if (processed > 0)
                {
                    logger.LogInformation("Processed {Count} extraction jobs.", processed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Processing worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

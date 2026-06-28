using CommerceSphere.ProductService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ProductService.Infrastructure.BulkImport;

// Single consumer of the in-process import queue. Processes one job at a time so a giant upload
// never starves the service's request threads or saturates the DB connection pool. Each job runs
// in its own DI scope (fresh DbContext / UnitOfWork), and a failure in one job never stops the loop.
public class BulkImportBackgroundWorker(
    IBulkImportQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BulkImportBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        logger.LogInformation("BulkImportBackgroundWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try
            {
                jobId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IProductImportService>();
                await service.ProcessJobAsync(jobId, $"bulk-import:{jobId}", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // ProcessJobAsync already marks the job Failed on its own exceptions; this only
                // catches scope/resolution faults so the loop keeps serving later jobs.
                logger.LogError(ex, "Unhandled error processing bulk import job {JobId}.", jobId);
            }
        }

        logger.LogInformation("BulkImportBackgroundWorker stopped.");
    }
}

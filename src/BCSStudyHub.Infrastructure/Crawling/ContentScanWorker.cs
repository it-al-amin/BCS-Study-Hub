using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BCSStudyHub.Infrastructure.Crawling;

/// <summary>
/// Picks up queued scans one at a time. The worker assumes a single API instance. The 12:00 AM scheduler (Phase 4) will enqueue scans that this worker then executes.
/// </summary>
internal sealed class ContentScanWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CrawlerOptions> options,
    TimeProvider timeProvider,
    ILogger<ContentScanWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            return;
        }

        await RecoverAsync(stoppingToken);
        var delay = TimeSpan.FromSeconds(Math.Max(options.Value.WorkerPollSeconds, 1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await RunNextAsync(stoppingToken))
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The content scan worker hit an error and will retry.");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Scans left "Running" by a crash or restart are marked failed so they can be retried.</summary>
    private async Task RecoverAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cutoff = timeProvider.GetUtcNow() - StaleAfter;
            var stale = await dbContext.ContentSourceScans
                .Where(scan => scan.Status == ContentScanStatus.Running && scan.StartedAt < cutoff)
                .ToListAsync(cancellationToken);
            foreach (var scan in stale)
            {
                scan.Status = ContentScanStatus.Failed;
                scan.ErrorMessage = "Interrupted before it finished (application restart).";
                scan.CompletedAt = timeProvider.GetUtcNow();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not recover interrupted scans.");
        }
    }

    internal async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var scan = await dbContext.ContentSourceScans
            .Where(row => row.Status == ContentScanStatus.Queued)
            .OrderBy(row => row.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (scan is null)
        {
            return false;
        }

        // Claim it. This assumes one API instance runs the worker (documented); running several needs a row-level lock.
        var scanId = scan.Id;
        scan.Status = ContentScanStatus.Running;
        scan.StartedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        var processor = scope.ServiceProvider.GetRequiredService<ContentScanProcessor>();
        await processor.ProcessAsync(scanId, cancellationToken);
        return true;
    }
}

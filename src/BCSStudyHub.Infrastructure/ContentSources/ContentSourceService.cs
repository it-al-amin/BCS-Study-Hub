using BCSStudyHub.Application.Admin;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.ContentSources;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Admin;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.ContentSources;

internal sealed class ContentSourceService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor accessor,
    TimeProvider timeProvider) : IContentSourceService
{
    public async Task<PagedResponse<ContentSourceDto>> ListAsync(ContentSourceQuery query, CancellationToken cancellationToken)
    {
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var sources = dbContext.ContentSources.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            sources = sources.Where(source =>
                source.Name.Contains(term) || (source.LocalizedName != null && source.LocalizedName.Contains(term)) || source.BaseUrl.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            sources = sources.Where(source => source.Category == query.Category);
        }

        if (ContentSourceInput.TryEnum<ContentSourceStatus>(query.Status, out var status))
        {
            sources = sources.Where(source => source.Status == status);
        }

        if (query.Enabled is bool enabled)
        {
            sources = sources.Where(source => source.IsEnabled == enabled);
        }

        var total = await sources.LongCountAsync(cancellationToken);
        var rows = await sources
            .OrderBy(source => source.Priority).ThenBy(source => source.Name)
            .Skip(page.Offset).Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<ContentSourceDto>(rows.Select(ToDto).ToList(), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<ContentSourceDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        ToDto(await FindAsync(id, cancellationToken, track: false));

    public async Task<ContentSourceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var sources = dbContext.ContentSources.AsNoTracking();
        var total = await sources.CountAsync(cancellationToken);
        var archived = await sources.CountAsync(source => source.Status == ContentSourceStatus.Archived, cancellationToken);
        var enabled = await sources.CountAsync(source => source.IsEnabled && source.Status == ContentSourceStatus.Active, cancellationToken);
        var attention = await sources.CountAsync(
            source => source.Status == ContentSourceStatus.Active
                      && (source.Health == ContentSourceHealth.NeedsAttention || source.Health == ContentSourceHealth.Failing),
            cancellationToken);
        var queued = await dbContext.ContentSourceScans.CountAsync(scan => scan.Status == ContentScanStatus.Queued, cancellationToken);
        var byCategory = await sources
            .Where(source => source.Status == ContentSourceStatus.Active)
            .GroupBy(source => source.Category)
            .Select(group => new { Category = group.Key, Count = group.Count() })
            .OrderBy(item => item.Category)
            .ToListAsync(cancellationToken);
        return new ContentSourceSummaryDto(
            total, enabled, total - archived - enabled, archived, attention, queued,
            byCategory.Select(item => new ContentSourceCategoryCount(item.Category, item.Count)).ToList());
    }

    public async Task<ContentSourceDto> CreateAsync(ContentSourceInput input, Guid actorId, CancellationToken cancellationToken)
    {
        AdminAudit.Validate(input);
        await EnsureUniqueBaseUrlAsync(input, null, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var source = new ContentSource
        {
            Id = Guid.NewGuid(),
            CreatedById = actorId,
            CreatedAt = now,
            IsEnabled = false, // A new source stays off until someone reviews its terms and turns it on.
        };
        Apply(source, input, now);
        dbContext.ContentSources.Add(source);
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.create", nameof(ContentSource), source.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(source);
    }

    public async Task<ContentSourceDto> UpdateAsync(Guid id, ContentSourceInput input, Guid actorId, CancellationToken cancellationToken)
    {
        AdminAudit.Validate(input);
        var source = await FindAsync(id, cancellationToken);
        await EnsureUniqueBaseUrlAsync(input, id, cancellationToken);
        Apply(source, input, timeProvider.GetUtcNow());
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.update", nameof(ContentSource), id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(source);
    }

    public async Task<ContentSourceDto> SetEnabledAsync(Guid id, bool enabled, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await FindAsync(id, cancellationToken);
        if (enabled && source.Status == ContentSourceStatus.Archived)
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Source archived", "Restore the source before enabling it.");
        }

        source.IsEnabled = enabled;
        source.UpdatedAt = timeProvider.GetUtcNow();
        AdminAudit.Add(dbContext, accessor, actorId, enabled ? "contentsource.enable" : "contentsource.disable", nameof(ContentSource), id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(source);
    }

    public async Task<ContentSourceDto> ArchiveAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await FindAsync(id, cancellationToken);
        source.Status = ContentSourceStatus.Archived;
        source.IsEnabled = false;
        source.UpdatedAt = timeProvider.GetUtcNow();
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.archive", nameof(ContentSource), id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(source);
    }

    public async Task<ContentSourceDto> RestoreAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await FindAsync(id, cancellationToken);
        source.Status = ContentSourceStatus.Active;
        source.UpdatedAt = timeProvider.GetUtcNow();
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.restore", nameof(ContentSource), id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(source);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await FindAsync(id, cancellationToken);
        if (source.Status != ContentSourceStatus.Archived)
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Archive first", "Only archived sources can be deleted.");
        }

        dbContext.ContentSources.Remove(source);
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.delete", nameof(ContentSource), id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContentSourceScanDto> RequestScanAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await FindAsync(id, cancellationToken);
        if (source.Status == ContentSourceStatus.Archived)
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Source archived", "Archived sources cannot be scanned.");
        }

        // One pending or running scan per source: a repeated click must not queue duplicates.
        var open = await dbContext.ContentSourceScans.AnyAsync(
            scan => scan.SourceId == id && (scan.Status == ContentScanStatus.Queued || scan.Status == ContentScanStatus.Running),
            cancellationToken);
        if (open)
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Scan already pending", "This source already has a queued or running scan.");
        }

        var scan = NewScan(source, ContentScanTrigger.Manual, actorId);
        dbContext.ContentSourceScans.Add(scan);
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.scan.request", nameof(ContentSource), id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(scan, source.Name);
    }

    public async Task<ContentSourceScanDto> RetryScanAsync(Guid scanId, Guid actorId, CancellationToken cancellationToken)
    {
        var failed = await dbContext.ContentSourceScans.Include(scan => scan.Source)
            .SingleOrDefaultAsync(scan => scan.Id == scanId, cancellationToken)
            ?? throw new AdminException(StatusCodes.Status404NotFound, "Scan not found", "The scan does not exist.");
        if (failed.Status is not (ContentScanStatus.Failed or ContentScanStatus.PartiallySucceeded or ContentScanStatus.Cancelled))
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Cannot retry", "Only failed, partial or cancelled scans can be retried.");
        }

        var created = await RequestScanCoreAsync(failed.Source, actorId, cancellationToken);
        return created;
    }

    public async Task<PagedResponse<ContentSourceScanDto>> ListScansAsync(
        Guid? sourceId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var page = new PageRequest(pageNumber, pageSize);
        var scans = dbContext.ContentSourceScans.AsNoTracking().AsQueryable();
        if (sourceId is Guid id)
        {
            scans = scans.Where(scan => scan.SourceId == id);
        }

        if (ContentSourceInput.TryEnum<ContentScanStatus>(status, out var parsed))
        {
            scans = scans.Where(scan => scan.Status == parsed);
        }

        var total = await scans.LongCountAsync(cancellationToken);
        var rows = await scans
            .OrderByDescending(scan => scan.RequestedAt)
            .Skip(page.Offset).Take(page.EffectivePageSize)
            .Select(scan => new { scan, scan.Source.Name })
            .ToListAsync(cancellationToken);
        return new PagedResponse<ContentSourceScanDto>(
            rows.Select(row => ToDto(row.scan, row.Name)).ToList(), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    private async Task<ContentSourceScanDto> RequestScanCoreAsync(ContentSource source, Guid actorId, CancellationToken cancellationToken)
    {
        var open = await dbContext.ContentSourceScans.AnyAsync(
            scan => scan.SourceId == source.Id && (scan.Status == ContentScanStatus.Queued || scan.Status == ContentScanStatus.Running),
            cancellationToken);
        if (open)
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Scan already pending", "This source already has a queued or running scan.");
        }

        var scan = NewScan(source, ContentScanTrigger.Retry, actorId);
        dbContext.ContentSourceScans.Add(scan);
        AdminAudit.Add(dbContext, accessor, actorId, "contentsource.scan.retry", nameof(ContentSource), source.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(scan, source.Name);
    }

    private ContentSourceScan NewScan(ContentSource source, ContentScanTrigger trigger, Guid actorId) => new()
    {
        Id = Guid.NewGuid(),
        SourceId = source.Id,
        Trigger = trigger,
        Status = ContentScanStatus.Queued,
        RequestedAt = timeProvider.GetUtcNow(),
        RequestedById = actorId,
    };

    private async Task EnsureUniqueBaseUrlAsync(ContentSourceInput input, Guid? excludeId, CancellationToken cancellationToken)
    {
        var url = input.BaseUrl.Trim();
        var duplicate = await dbContext.ContentSources.AnyAsync(
            source => source.BaseUrl == url && (excludeId == null || source.Id != excludeId), cancellationToken);
        if (duplicate)
        {
            throw new AdminException(StatusCodes.Status409Conflict, "Duplicate source", "A source with this base URL already exists.");
        }
    }

    private async Task<ContentSource> FindAsync(Guid id, CancellationToken cancellationToken, bool track = true)
    {
        var query = track ? dbContext.ContentSources : dbContext.ContentSources.AsNoTracking();
        return await query.SingleOrDefaultAsync(source => source.Id == id, cancellationToken)
               ?? throw new AdminException(StatusCodes.Status404NotFound, "Source not found", "The content source does not exist.");
    }

    private static void Apply(ContentSource source, ContentSourceInput input, DateTimeOffset now)
    {
        source.Name = input.Name.Trim();
        source.LocalizedName = Normalize(input.LocalizedName);
        source.SourceType = Enum.Parse<ContentSourceType>(input.SourceType, true);
        source.ExtractionMethod = Enum.Parse<ContentExtractionMethod>(input.ExtractionMethod, true);
        source.BaseUrl = input.BaseUrl.Trim();
        source.FeedUrl = Normalize(input.FeedUrl);
        source.SitemapUrl = Normalize(input.SitemapUrl);
        source.Category = input.Category.Trim();
        source.Language = input.Language;
        source.Priority = input.Priority;
        source.Reliability = input.Reliability;
        source.ScanIntervalMinutes = input.ScanIntervalMinutes;
        source.RetentionPolicy = Enum.Parse<ContentRetentionPolicy>(input.RetentionPolicy, true);
        source.IncludePatterns = Normalize(input.IncludePatterns);
        source.ExcludePatterns = Normalize(input.ExcludePatterns);
        source.RequiresVerification = input.RequiresVerification;
        source.RightsNote = Normalize(input.RightsNote);
        source.Notes = Normalize(input.Notes);
        source.UpdatedAt = now;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContentSourceDto ToDto(ContentSource source) => new(
        source.Id, source.Name, source.LocalizedName, source.SourceType.ToString(), source.ExtractionMethod.ToString(),
        source.BaseUrl, source.FeedUrl, source.SitemapUrl, source.Category, source.Language, source.Priority,
        source.Reliability, source.ScanIntervalMinutes, source.RetentionPolicy.ToString(), source.IncludePatterns,
        source.ExcludePatterns, source.RequiresVerification, source.RightsNote, source.Notes, source.IsEnabled,
        source.Status.ToString(), source.Health.ToString(), source.LastScanAt, source.LastSuccessAt, source.LastError,
        source.ConsecutiveFailures, source.CreatedAt, source.UpdatedAt);

    private static ContentSourceScanDto ToDto(ContentSourceScan scan, string sourceName) => new(
        scan.Id, scan.SourceId, sourceName, scan.Trigger.ToString(), scan.Status.ToString(), scan.RequestedAt,
        scan.StartedAt, scan.CompletedAt, scan.DiscoveredCount, scan.UpdatedCount, scan.UnchangedCount,
        scan.FailedCount, scan.ErrorMessage);
}

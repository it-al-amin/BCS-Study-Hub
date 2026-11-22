using BCSStudyHub.Application.Admin;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.ContentSources;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.ContentSources;

internal sealed class DiscoveredContentService(ApplicationDbContext dbContext) : IDiscoveredContentService
{
    public async Task<PagedResponse<DiscoveredContentDto>> ListAsync(DiscoveredContentQuery query, CancellationToken cancellationToken)
    {
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = dbContext.DiscoveredContents.AsNoTracking().AsQueryable();
        if (query.SourceId is Guid sourceId)
        {
            rows = rows.Where(row => row.SourceId == sourceId);
        }

        if (ContentSourceInput.TryEnum<DiscoveredContentStatus>(query.Status, out var status))
        {
            rows = rows.Where(row => row.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(row => row.Title.Contains(term) || row.CanonicalUrl.Contains(term));
        }

        var total = await rows.LongCountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(row => row.DiscoveredAt)
            .Skip(page.Offset).Take(page.EffectivePageSize)
            .Select(row => new { row, SourceName = row.Source.Name })
            .ToListAsync(cancellationToken);
        return new PagedResponse<DiscoveredContentDto>(
            items.Select(item => ToDto(item.row, item.SourceName)).ToList(), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<DiscoveredContentDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.DiscoveredContents.AsNoTracking().Include(content => content.Source)
            .SingleOrDefaultAsync(content => content.Id == id, cancellationToken)
            ?? throw new AdminException(StatusCodes.Status404NotFound, "Content not found", "The discovered content does not exist.");
        var versions = await dbContext.DiscoveredContentVersions.AsNoTracking()
            .Where(version => version.ContentId == id)
            .OrderByDescending(version => version.Version)
            .ToListAsync(cancellationToken);
        return new DiscoveredContentDetailDto(
            ToDto(row, row.Source.Name),
            row.TextContent,
            versions.Select(version => new DiscoveredContentVersionDto(
                version.Version, version.Title, version.Excerpt, version.ContentHash, version.CapturedAt, version.ScanId)).ToList());
    }

    private static DiscoveredContentDto ToDto(DiscoveredContent row, string sourceName) => new(
        row.Id, row.SourceId, sourceName, row.CanonicalUrl, row.Title, row.Language, row.Kind.ToString(), row.PublishedAt,
        row.Excerpt, row.TextContent is not null, row.Version, row.Status.ToString(), row.Visibility.ToString(),
        row.DuplicateOfId, row.DiscoveredAt, row.LastSeenAt, row.UpdatedAt);
}

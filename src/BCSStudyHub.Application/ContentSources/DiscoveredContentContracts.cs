using BCSStudyHub.Application.Common;

namespace BCSStudyHub.Application.ContentSources;

public sealed record DiscoveredContentDto(
    Guid Id,
    Guid SourceId,
    string SourceName,
    string CanonicalUrl,
    string Title,
    string? Language,
    string Kind,
    DateTimeOffset? PublishedAt,
    string? Excerpt,
    bool HasFullText,
    int Version,
    string Status,
    string Visibility,
    Guid? DuplicateOfId,
    DateTimeOffset DiscoveredAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset UpdatedAt);

public sealed record DiscoveredContentVersionDto(int Version, string Title, string? Excerpt, string ContentHash, DateTimeOffset CapturedAt, Guid? ScanId);

public sealed record DiscoveredContentDetailDto(
    DiscoveredContentDto Content,
    string? TextContent,
    IReadOnlyList<DiscoveredContentVersionDto> Versions);

public sealed record DiscoveredContentQuery(Guid? SourceId, string? Status, string? Search, int PageNumber = 1, int PageSize = 25);

public interface IDiscoveredContentService
{
    Task<PagedResponse<DiscoveredContentDto>> ListAsync(DiscoveredContentQuery query, CancellationToken cancellationToken);
    Task<DiscoveredContentDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);
}

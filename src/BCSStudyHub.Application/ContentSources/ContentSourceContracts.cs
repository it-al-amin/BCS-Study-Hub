using System.ComponentModel.DataAnnotations;
using System.Net;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.ContentSources;

/// <summary>
/// Static, DNS-free checks on a configured source URL. They stop obviously unsafe targets at configuration time;
/// the fetcher (Phase 2) must repeat resolution-based checks (resolved IP, every redirect) before any request.
/// </summary>
public static class SourceUrlRules
{
    private static readonly string[] BlockedHostSuffixes = [".local", ".internal", ".localhost", ".lan", ".home.arpa"];
    private static readonly string[] BlockedHosts = ["localhost", "metadata.google.internal", "metadata"];

    public static string? Validate(string? url, bool required, string field)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return required ? $"{field} is required." : null;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return $"{field} must be an absolute https:// URL.";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return $"{field} must not contain credentials.";
        }

        if (uri.Port != 443)
        {
            return $"{field} must use the default https port.";
        }

        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (BlockedHosts.Contains(host) || BlockedHostSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.Ordinal)))
        {
            return $"{field} points to a host that is not allowed.";
        }

        if (IPAddress.TryParse(host.Trim('[', ']'), out var address))
        {
            return IsPublic(address) ? null : $"{field} points to a private or reserved address.";
        }

        return host.Contains('.') ? null : $"{field} must use a fully qualified public host name.";
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            return !(bytes[0] is 0 or 10 or 127
                     || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
                     || (bytes[0] == 169 && bytes[1] == 254)
                     || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                     || (bytes[0] == 192 && bytes[1] == 168)
                     || (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
                     || bytes[0] >= 224);
        }

        return bytes.Length == 16 && (bytes[0] & 0xFE) != 0xFC;
    }
}

public sealed record ContentSourceInput : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(200)]
    public string? LocalizedName { get; init; }

    [Required]
    public string SourceType { get; init; } = nameof(ContentSourceType.Website);

    [Required]
    public string ExtractionMethod { get; init; } = nameof(ContentExtractionMethod.ManualOnly);

    [Required, StringLength(512)]
    public string BaseUrl { get; init; } = string.Empty;

    [StringLength(512)]
    public string? FeedUrl { get; init; }

    [StringLength(512)]
    public string? SitemapUrl { get; init; }

    [Required, StringLength(60, MinimumLength = 2)]
    public string Category { get; init; } = "General";

    [Required]
    public string Language { get; init; } = "bn";

    [Range(1, 1000)]
    public int Priority { get; init; } = 100;

    [Range(1, 5)]
    public int Reliability { get; init; } = 3;

    [Range(15, 43200)]
    public int ScanIntervalMinutes { get; init; } = 1440;

    [Required]
    public string RetentionPolicy { get; init; } = nameof(ContentRetentionPolicy.LinkAndMetadataOnly);

    [StringLength(2000)]
    public string? IncludePatterns { get; init; }

    [StringLength(2000)]
    public string? ExcludePatterns { get; init; }

    public bool RequiresVerification { get; init; } = true;

    [StringLength(1000)]
    public string? RightsNote { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryEnum<ContentSourceType>(SourceType, out _))
        {
            yield return new ValidationResult("Unknown source type.", [nameof(SourceType)]);
        }

        var methodValid = TryEnum<ContentExtractionMethod>(ExtractionMethod, out var method);
        if (!methodValid)
        {
            yield return new ValidationResult("Unknown extraction method.", [nameof(ExtractionMethod)]);
        }

        var retentionValid = TryEnum<ContentRetentionPolicy>(RetentionPolicy, out var retention);
        if (!retentionValid)
        {
            yield return new ValidationResult("Unknown retention policy.", [nameof(RetentionPolicy)]);
        }

        if (Language is not ("bn" or "en" or "mixed"))
        {
            yield return new ValidationResult("Language must be bn, en or mixed.", [nameof(Language)]);
        }

        foreach (var (value, required, name) in new[]
                 {
                     (BaseUrl, true, nameof(BaseUrl)),
                     (FeedUrl, false, nameof(FeedUrl)),
                     (SitemapUrl, false, nameof(SitemapUrl)),
                 })
        {
            if (SourceUrlRules.Validate(value, required, name) is { } error)
            {
                yield return new ValidationResult(error, [name]);
            }
        }

        if (methodValid && method == ContentExtractionMethod.Feed && string.IsNullOrWhiteSpace(FeedUrl))
        {
            yield return new ValidationResult("Feed extraction needs a feed URL.", [nameof(FeedUrl)]);
        }

        if (methodValid && method == ContentExtractionMethod.Sitemap && string.IsNullOrWhiteSpace(SitemapUrl))
        {
            yield return new ValidationResult("Sitemap extraction needs a sitemap URL.", [nameof(SitemapUrl)]);
        }

        if (retentionValid && retention == ContentRetentionPolicy.FullTextPermitted && string.IsNullOrWhiteSpace(RightsNote))
        {
            yield return new ValidationResult(
                "Keeping full text needs a rights note explaining the permission or licence.", [nameof(RightsNote)]);
        }
    }

    public static bool TryEnum<T>(string? value, out T result) where T : struct, Enum =>
        Enum.TryParse(value, true, out result) && Enum.IsDefined(result) && !int.TryParse(value, out _);
}

public sealed record ContentSourceDto(
    Guid Id,
    string Name,
    string? LocalizedName,
    string SourceType,
    string ExtractionMethod,
    string BaseUrl,
    string? FeedUrl,
    string? SitemapUrl,
    string Category,
    string Language,
    int Priority,
    int Reliability,
    int ScanIntervalMinutes,
    string RetentionPolicy,
    string? IncludePatterns,
    string? ExcludePatterns,
    bool RequiresVerification,
    string? RightsNote,
    string? Notes,
    bool IsEnabled,
    string Status,
    string Health,
    DateTimeOffset? LastScanAt,
    DateTimeOffset? LastSuccessAt,
    string? LastError,
    int ConsecutiveFailures,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ContentSourceScanDto(
    Guid Id,
    Guid SourceId,
    string SourceName,
    string Trigger,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int DiscoveredCount,
    int UpdatedCount,
    int UnchangedCount,
    int FailedCount,
    string? ErrorMessage);

public sealed record ContentSourceCategoryCount(string Category, int Count);

public sealed record ContentSourceSummaryDto(
    int Total,
    int Enabled,
    int Disabled,
    int Archived,
    int NeedsAttention,
    int QueuedScans,
    IReadOnlyList<ContentSourceCategoryCount> ByCategory);

public sealed record ContentSourceQuery(
    string? Search,
    string? Category,
    string? Status,
    bool? Enabled,
    int PageNumber = 1,
    int PageSize = 25);

public interface IContentSourceService
{
    Task<PagedResponse<ContentSourceDto>> ListAsync(ContentSourceQuery query, CancellationToken cancellationToken);
    Task<ContentSourceDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ContentSourceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    Task<ContentSourceDto> CreateAsync(ContentSourceInput input, Guid actorId, CancellationToken cancellationToken);
    Task<ContentSourceDto> UpdateAsync(Guid id, ContentSourceInput input, Guid actorId, CancellationToken cancellationToken);
    Task<ContentSourceDto> SetEnabledAsync(Guid id, bool enabled, Guid actorId, CancellationToken cancellationToken);
    Task<ContentSourceDto> ArchiveAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
    Task<ContentSourceDto> RestoreAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
    Task<ContentSourceScanDto> RequestScanAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
    Task<ContentSourceScanDto> RetryScanAsync(Guid scanId, Guid actorId, CancellationToken cancellationToken);
    Task<PagedResponse<ContentSourceScanDto>> ListScansAsync(Guid? sourceId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

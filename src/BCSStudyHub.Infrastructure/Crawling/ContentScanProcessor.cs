using System.Text;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BCSStudyHub.Infrastructure.Crawling;

/// <summary>Runs one queued scan: discovers items through the source's adapter and records them. A failure never escapes.</summary>
internal sealed class ContentScanProcessor(
    ApplicationDbContext dbContext,
    ISafeHttpFetcher fetcher,
    RobotsService robots,
    IOptions<CrawlerOptions> options,
    TimeProvider timeProvider,
    ILogger<ContentScanProcessor> logger)
{
    private const int MaxFullTextCharacters = 200_000;
    private const int ExcerptCharacters = 500;
    private readonly CrawlerOptions settings = options.Value;

    public async Task ProcessAsync(Guid scanId, CancellationToken cancellationToken)
    {
        var scan = await dbContext.ContentSourceScans.Include(row => row.Source)
            .SingleOrDefaultAsync(row => row.Id == scanId, cancellationToken);
        if (scan is null || scan.Status != ContentScanStatus.Running)
        {
            return;
        }

        var source = scan.Source;
        var counts = new Counts();
        string? error = null;
        try
        {
            var items = await DiscoverAsync(source, counts, cancellationToken);
            foreach (var item in items.Take(settings.MaxItemsPerScan))
            {
                Upsert(source, scan, item, counts);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            scan.Status = ContentScanStatus.Cancelled;
            scan.ErrorMessage = "Cancelled because the application is stopping.";
            scan.CompletedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(CancellationToken.None);
            return;
        }
        catch (Exception exception)
        {
            error = exception is FetchException ? exception.Message : "Unexpected error while scanning.";
            logger.LogWarning(exception, "Scan {ScanId} for source {Source} failed.", scan.Id, source.Name);
        }

        Finish(source, scan, counts, error);
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<List<DiscoveredItem>> DiscoverAsync(ContentSource source, Counts counts, CancellationToken cancellationToken)
    {
        switch (source.ExtractionMethod)
        {
            case ContentExtractionMethod.Feed:
                return await DiscoverFeedAsync(source, cancellationToken);
            case ContentExtractionMethod.Sitemap:
                return await DiscoverSitemapAsync(source, cancellationToken);
            case ContentExtractionMethod.HtmlExtraction:
                return await DiscoverHtmlAsync(source, counts, cancellationToken);
            case ContentExtractionMethod.ManualOnly:
                throw new FetchException("This source is set to manual only; choose a feed, sitemap or HTML extraction method to scan it.");
            default:
                throw new FetchException($"The '{source.ExtractionMethod}' method is not available yet.");
        }
    }

    private async Task<List<DiscoveredItem>> DiscoverFeedAsync(ContentSource source, CancellationToken cancellationToken)
    {
        var feedUrl = new Uri(source.FeedUrl ?? throw new FetchException("The source has no feed URL."));
        await EnsureAllowedAsync(feedUrl, cancellationToken);
        var result = await fetcher.FetchAsync(feedUrl, new FetchOptions(AcceptedContentTypes: XmlTypes), cancellationToken);
        return Filter(source, FeedParser.Parse(result.Body, result.FinalUri, LanguageHint(source)));
    }

    private async Task<List<DiscoveredItem>> DiscoverSitemapAsync(ContentSource source, CancellationToken cancellationToken)
    {
        var start = new Uri(source.SitemapUrl ?? throw new FetchException("The source has no sitemap URL."));
        var host = new Uri(source.BaseUrl).IdnHost;
        var entries = new List<SitemapParser.Entry>();
        var queue = new Queue<Uri>([start]);
        var visited = 0;
        while (queue.Count > 0 && visited++ < 6 && entries.Count < settings.MaxItemsPerScan)
        {
            var current = queue.Dequeue();
            if (!current.IdnHost.Equals(host, StringComparison.OrdinalIgnoreCase))
            {
                continue; // A sitemap may only list its own host.
            }

            await EnsureAllowedAsync(current, cancellationToken);
            var fetched = await fetcher.FetchAsync(current, new FetchOptions(AcceptedContentTypes: XmlTypes), cancellationToken);
            var parsed = SitemapParser.Parse(fetched.Body);
            entries.AddRange(parsed.Urls.Where(entry => entry.Location.IdnHost.Equals(host, StringComparison.OrdinalIgnoreCase)));
            foreach (var child in parsed.ChildSitemaps)
            {
                queue.Enqueue(child);
            }
        }

        var items = entries
            .OrderByDescending(entry => entry.LastModified ?? DateTimeOffset.MinValue)
            .Select(entry => new DiscoveredItem(entry.Location, null, null, entry.LastModified, LanguageHint(source), null, KindOf(entry.Location)))
            .ToList();
        return Filter(source, items);
    }

    private async Task<List<DiscoveredItem>> DiscoverHtmlAsync(ContentSource source, Counts counts, CancellationToken cancellationToken)
    {
        var baseUrl = new Uri(source.BaseUrl);
        await EnsureAllowedAsync(baseUrl, cancellationToken);
        var listing = await fetcher.FetchAsync(baseUrl, new FetchOptions(AcceptedContentTypes: HtmlTypes), cancellationToken);
        var page = HtmlPage.Extract(Encoding.UTF8.GetString(listing.Body), listing.FinalUri);
        var candidates = page.Links
            .Where(link => link.IdnHost.Equals(baseUrl.IdnHost, StringComparison.OrdinalIgnoreCase)
                           && UrlPatternFilter.Matches(link, source.IncludePatterns, source.ExcludePatterns))
            .Take(settings.MaxPagesFetchedPerScan)
            .ToList();

        var items = new List<DiscoveredItem>();
        foreach (var link in candidates)
        {
            try
            {
                if (!await robots.IsAllowedAsync(link, cancellationToken))
                {
                    continue;
                }

                var fetched = await fetcher.FetchAsync(link, new FetchOptions(AcceptedContentTypes: HtmlTypes), cancellationToken);
                var extracted = HtmlPage.Extract(Encoding.UTF8.GetString(fetched.Body), fetched.FinalUri);
                var url = Uri.TryCreate(fetched.FinalUri, extracted.Canonical, out var canonical)
                          && canonical.Scheme == Uri.UriSchemeHttps
                          && canonical.IdnHost.Equals(baseUrl.IdnHost, StringComparison.OrdinalIgnoreCase)
                    ? canonical
                    : fetched.FinalUri;
                items.Add(new DiscoveredItem(
                    url, extracted.Title, extracted.Description, extracted.Published,
                    extracted.Language ?? LanguageHint(source), extracted.Text, DiscoveredContentKind.Article));
            }
            catch (FetchException exception)
            {
                counts.Failed++;
                logger.LogInformation("Skipping {Url}: {Message}", link, exception.Message);
            }
        }

        return items;
    }

    private async Task EnsureAllowedAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!await robots.IsAllowedAsync(uri, cancellationToken))
        {
            throw new FetchException($"robots.txt does not allow fetching {uri.AbsolutePath}.");
        }
    }

    private static List<DiscoveredItem> Filter(ContentSource source, IEnumerable<DiscoveredItem> items) =>
        items.Where(item => UrlPatternFilter.Matches(item.Url, source.IncludePatterns, source.ExcludePatterns)).ToList();

    private static string? LanguageHint(ContentSource source) => source.Language == "mixed" ? null : source.Language;

    private static DiscoveredContentKind KindOf(Uri url) =>
        url.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? DiscoveredContentKind.Document : DiscoveredContentKind.Article;

    private void Upsert(ContentSource source, ContentSourceScan scan, DiscoveredItem item, Counts counts)
    {
        var now = timeProvider.GetUtcNow();
        var canonical = UrlCanonicalizer.Canonicalize(item.Url);
        var urlHash = UrlCanonicalizer.Hash(canonical);
        var title = Truncate(HtmlText.Normalize(item.Title ?? canonical), 500);
        var text = item.Text is null ? null : Truncate(HtmlText.Normalize(item.Text), MaxFullTextCharacters);
        var contentHash = UrlCanonicalizer.Hash(title + "\n" + (text ?? item.Summary ?? string.Empty));

        var (excerpt, fullText) = source.RetentionPolicy switch
        {
            ContentRetentionPolicy.FullTextPermitted => (Truncate(item.Summary ?? text, ExcerptCharacters), text),
            ContentRetentionPolicy.ExcerptAndSummary => (Truncate(item.Summary ?? text, ExcerptCharacters), (string?)null),
            _ => ((string?)null, (string?)null),
        };

        var existing = dbContext.DiscoveredContents.Local.FirstOrDefault(row => row.SourceId == source.Id && row.UrlHash == urlHash)
                       ?? dbContext.DiscoveredContents.FirstOrDefault(row => row.SourceId == source.Id && row.UrlHash == urlHash);
        if (existing is null)
        {
            var created = new DiscoveredContent
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                CanonicalUrl = Truncate(canonical, 1000)!,
                UrlHash = urlHash,
                Title = title!,
                Language = item.Language,
                Kind = item.Kind,
                PublishedAt = item.PublishedAt,
                Excerpt = excerpt,
                TextContent = fullText,
                ContentHash = contentHash,
                Version = 1,
                Status = source.RequiresVerification ? DiscoveredContentStatus.PendingReview : DiscoveredContentStatus.Discovered,
                Visibility = ContentVisibility.Private,
                DiscoveredAt = now,
                LastSeenAt = now,
                UpdatedAt = now,
            };
            if (text is { Length: > 200 })
            {
                created.DuplicateOfId = dbContext.DiscoveredContents
                    .Where(row => row.ContentHash == contentHash && row.UrlHash != urlHash)
                    .OrderBy(row => row.DiscoveredAt).Select(row => (Guid?)row.Id).FirstOrDefault();
                if (created.DuplicateOfId is not null)
                {
                    created.Status = DiscoveredContentStatus.Duplicate;
                }
            }

            dbContext.DiscoveredContents.Add(created);
            dbContext.DiscoveredContentVersions.Add(Snapshot(created, scan.Id, now));
            counts.Discovered++;
            return;
        }

        existing.LastSeenAt = now;
        if (existing.ContentHash == contentHash || (text is null && item.Summary is null && existing.Title == title))
        {
            counts.Unchanged++;
            return;
        }

        existing.Version++;
        existing.Title = title!;
        existing.Excerpt = excerpt;
        existing.TextContent = fullText;
        existing.ContentHash = contentHash;
        existing.PublishedAt ??= item.PublishedAt;
        existing.UpdatedAt = now;
        if (existing.Status is DiscoveredContentStatus.Verified or DiscoveredContentStatus.Published)
        {
            existing.Status = DiscoveredContentStatus.Updated; // Changed after review, so it needs another look.
        }

        dbContext.DiscoveredContentVersions.Add(Snapshot(existing, scan.Id, now));
        counts.Updated++;
    }

    private static DiscoveredContentVersion Snapshot(DiscoveredContent content, Guid scanId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        ContentId = content.Id,
        Version = content.Version,
        Title = content.Title,
        Excerpt = content.Excerpt,
        TextContent = content.TextContent,
        ContentHash = content.ContentHash,
        CapturedAt = now,
        ScanId = scanId,
    };

    private void Finish(ContentSource source, ContentSourceScan scan, Counts counts, string? error)
    {
        var now = timeProvider.GetUtcNow();
        scan.DiscoveredCount = counts.Discovered;
        scan.UpdatedCount = counts.Updated;
        scan.UnchangedCount = counts.Unchanged;
        scan.FailedCount = counts.Failed + (error is null ? 0 : 1);
        scan.ErrorMessage = error;
        scan.CompletedAt = now;
        var productive = counts.Discovered + counts.Updated + counts.Unchanged > 0;
        scan.Status = error is null
            ? (counts.Failed > 0 ? ContentScanStatus.PartiallySucceeded : ContentScanStatus.Succeeded)
            : (productive ? ContentScanStatus.PartiallySucceeded : ContentScanStatus.Failed);

        source.LastScanAt = now;
        if (scan.Status is ContentScanStatus.Succeeded or ContentScanStatus.PartiallySucceeded && error is null)
        {
            source.LastSuccessAt = now;
            source.ConsecutiveFailures = 0;
            source.LastError = null;
            source.Health = counts.Failed > 0 ? ContentSourceHealth.NeedsAttention : ContentSourceHealth.Healthy;
        }
        else
        {
            source.ConsecutiveFailures++;
            source.LastError = Truncate(error, 1000);
            source.Health = source.ConsecutiveFailures >= 3 ? ContentSourceHealth.Failing : ContentSourceHealth.NeedsAttention;
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    private static readonly string[] XmlTypes =
        ["application/xml", "text/xml", "application/rss+xml", "application/atom+xml", "application/rdf+xml", "text/plain"];

    private static readonly string[] HtmlTypes = ["text/html", "application/xhtml+xml"];

    private sealed class Counts
    {
        public int Discovered;
        public int Updated;
        public int Unchanged;
        public int Failed;
    }
}

using System.Net;
using System.Text;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Crawling;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BCSStudyHub.Api.Tests;

public sealed class CrawlingParsingTests
{
    [Fact]
    public void RobotsLongestRuleWinsAndWildcardsWork()
    {
        var rules = RobotsRules.Parse("User-agent: *\nDisallow: /private\nAllow: /private/open\nDisallow: /*.pdf$\nCrawl-delay: 5", "BCSStudyHubBot");
        Assert.False(rules.IsAllowed("/private/data"));
        Assert.True(rules.IsAllowed("/private/open/page"));
        Assert.False(rules.IsAllowed("/files/a.pdf"));
        Assert.True(rules.IsAllowed("/files/a.pdf.html"));
        Assert.True(rules.IsAllowed("/news"));
        Assert.Equal(TimeSpan.FromSeconds(5), rules.CrawlDelay);
    }

    [Fact]
    public void RobotsPrefersOurAgentGroupAndEmptyDisallowAllowsAll()
    {
        var text = "User-agent: *\nDisallow: /\n\nUser-agent: BCSStudyHubBot\nDisallow:\n";
        Assert.True(RobotsRules.Parse(text, "BCSStudyHubBot").IsAllowed("/anything"));
        Assert.False(RobotsRules.Parse(text, "OtherBot").IsAllowed("/anything"));
        Assert.False(RobotsRules.DisallowAll.IsAllowed("/x"));
        Assert.True(RobotsRules.AllowAll.IsAllowed("/x"));
    }

    [Fact]
    public void CanonicalUrlsDropFragmentsTrackingAndOrderQuery()
    {
        var one = UrlCanonicalizer.Canonicalize(new Uri("https://Example.com:443/news/?b=2&utm_source=x&a=1#top"));
        var two = UrlCanonicalizer.Canonicalize(new Uri("https://example.com/news?a=1&b=2"));
        Assert.Equal(one, two);
        Assert.Equal(64, UrlCanonicalizer.Hash(one).Length);
    }

    [Fact]
    public void UrlPatternsIncludeAndExclude()
    {
        var url = new Uri("https://example.com/notices/2026/a.html");
        Assert.True(UrlPatternFilter.Matches(url, null, null));
        Assert.True(UrlPatternFilter.Matches(url, "/notices/*", null));
        Assert.False(UrlPatternFilter.Matches(url, "/news/*", null));
        Assert.False(UrlPatternFilter.Matches(url, null, "2026"));
    }

    [Fact]
    public void RssAndAtomFeedsAreParsed()
    {
        const string rss = "<rss version=\"2.0\"><channel>"
            + "<item><title>Exam &amp; notice</title><link>https://example.com/a</link><description>&lt;p&gt;Hello&lt;/p&gt;</description><pubDate>Mon, 02 Nov 2026 10:00:00 GMT</pubDate></item>"
            + "<item><title>Insecure</title><link>http://example.com/b</link></item>"
            + "</channel></rss>";
        var items = FeedParser.Parse(Encoding.UTF8.GetBytes(rss), new Uri("https://example.com/feed"), "bn");
        var item = Assert.Single(items);
        Assert.Equal("Exam & notice", item.Title);
        Assert.Equal("Hello", item.Summary);
        Assert.Equal(2026, item.PublishedAt!.Value.Year);

        const string atom = "<feed xmlns=\"http://www.w3.org/2005/Atom\"><entry><title>Atom one</title>"
            + "<link rel=\"alternate\" href=\"/atom/1\"/><updated>2026-11-03T08:00:00Z</updated><summary>S</summary></entry></feed>";
        var entry = Assert.Single(FeedParser.Parse(Encoding.UTF8.GetBytes(atom), new Uri("https://example.com/"), null));
        Assert.Equal("https://example.com/atom/1", entry.Url.AbsoluteUri);
    }

    [Fact]
    public void XmlWithDtdIsRejected()
    {
        const string xxe = "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]><rss><channel><item><title>&e;</title></item></channel></rss>";
        Assert.ThrowsAny<Exception>(() => FeedParser.Parse(Encoding.UTF8.GetBytes(xxe), new Uri("https://example.com"), null));
    }

    [Fact]
    public void SitemapsAndIndexesAreParsed()
    {
        const string urlset = "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"
            + "<url><loc>https://example.com/a</loc><lastmod>2026-11-01</lastmod></url>"
            + "<url><loc>http://example.com/insecure</loc></url></urlset>";
        var result = SitemapParser.Parse(Encoding.UTF8.GetBytes(urlset));
        Assert.Single(result.Urls);
        const string index = "<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><sitemap><loc>https://example.com/s1.xml</loc></sitemap></sitemapindex>";
        Assert.Single(SitemapParser.Parse(Encoding.UTF8.GetBytes(index)).ChildSitemaps);
    }

    [Fact]
    public void HtmlPageExtractsMetadataMainTextAndSameSiteLinks()
    {
        const string html = "<html lang=\"bn\"><head><title>Page title</title><meta name=\"description\" content=\"A &amp; B\">"
            + "<link rel=\"canonical\" href=\"https://example.com/canon\"></head>"
            + "<body><nav>Menu</nav><script>alert(1)</script>"
            + "<article><h1>Heading</h1><p>Body text here.</p><a href=\"/next?utm_source=z#x\">Next</a></article>"
            + "<a href=\"javascript:void(0)\">x</a><a href=\"/logo.png\">img</a><a href=\"http://insecure.test/p\">insecure</a></body></html>";
        var page = HtmlPage.Extract(html, new Uri("https://example.com/start"));
        Assert.Equal("Page title", page.Title);
        Assert.Equal("A & B", page.Description);
        Assert.Equal("bn", page.Language);
        Assert.Equal("https://example.com/canon", page.Canonical);
        Assert.Equal("Heading Body text here. Next", page.Text);
        Assert.DoesNotContain("Menu", page.Text);
        Assert.Single(page.Links);
    }
}

public sealed class FetcherSafetyTests
{
    private static SafeHttpFetcher Create(Func<HttpRequestMessage, HttpResponseMessage> respond, int maxBytes = 1000, int maxRedirects = 2) =>
        new(Options.Create(new CrawlerOptions { MinHostIntervalMilliseconds = 0, MaxBytes = maxBytes, MaxRedirects = maxRedirects }),
            TimeProvider.System, new StubHandler(respond));

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private static HttpResponseMessage Html(string body, string type = "text/html") =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, type) };

    private static HttpResponseMessage Redirect(string to) =>
        new(HttpStatusCode.Found) { Headers = { Location = new Uri(to) } };

    [Fact]
    public async Task ReturnsBodyForAValidPage()
    {
        using var fetcher = Create(_ => Html("<p>hi</p>"));
        var result = await fetcher.FetchAsync(new Uri("https://example.com/"), null, default);
        Assert.Equal("text/html", result.ContentType);
        Assert.Equal("<p>hi</p>", Encoding.UTF8.GetString(result.Body));
    }

    [Theory]
    [InlineData("http://example.com/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("https://169.254.169.254/latest")]
    [InlineData("https://localhost/")]
    public async Task UnsafeStartUrlsNeverReachTheNetwork(string url)
    {
        var calls = 0;
        using var fetcher = Create(_ => { calls++; return Html("x"); });
        await Assert.ThrowsAsync<FetchException>(() => fetcher.FetchAsync(new Uri(url), null, default));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task RedirectToAnInternalAddressIsBlocked()
    {
        using var fetcher = Create(request => request.RequestUri!.Host == "example.com" ? Redirect("https://10.0.0.7/admin") : Html("secret"));
        var error = await Assert.ThrowsAsync<FetchException>(() => fetcher.FetchAsync(new Uri("https://example.com/"), null, default));
        Assert.Contains("private", error.Message);
    }

    [Fact]
    public async Task RedirectLoopsStopAtTheLimit()
    {
        var calls = 0;
        using var fetcher = Create(_ => { calls++; return Redirect("https://example.com/again"); }, maxRedirects: 2);
        await Assert.ThrowsAsync<FetchException>(() => fetcher.FetchAsync(new Uri("https://example.com/"), null, default));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task OversizeAndWrongTypeResponsesAreRejected()
    {
        using var big = Create(_ => Html(new string('a', 5000)), maxBytes: 1000);
        await Assert.ThrowsAsync<FetchException>(() => big.FetchAsync(new Uri("https://example.com/"), null, default));
        using var binary = Create(_ => Html("x", "application/zip"));
        await Assert.ThrowsAsync<FetchException>(() => binary.FetchAsync(new Uri("https://example.com/"), null, default));
    }

    [Fact]
    public async Task ErrorStatusesAreReportedAndTransientOnesFlagged()
    {
        using var fetcher = Create(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var error = await Assert.ThrowsAsync<FetchException>(() => fetcher.FetchAsync(new Uri("https://example.com/"), null, default));
        Assert.True(error.Transient);
        Assert.Equal(503, error.StatusCode);
    }

    [Fact]
    public async Task NotModifiedIsReportedWithoutABody()
    {
        using var fetcher = Create(_ => new HttpResponseMessage(HttpStatusCode.NotModified));
        var result = await fetcher.FetchAsync(new Uri("https://example.com/"), new FetchOptions(ETag: "\"abc\""), default);
        Assert.True(result.NotModified);
    }

    [Fact]
    public async Task HostsResolvingToPrivateAddressesAreBlocked()
    {
        static Task<IPAddress[]> Resolve(string host, CancellationToken token) => Task.FromResult(host switch
        {
            "good.test" => new[] { IPAddress.Parse("93.184.216.34") },
            "mixed.test" => new[] { IPAddress.Parse("93.184.216.34"), IPAddress.Parse("10.0.0.1") },
            _ => new[] { IPAddress.Parse("127.0.0.1") },
        });

        Assert.Single(await PublicAddressGuard.ResolvePublicAsync("good.test", Resolve, default));
        await Assert.ThrowsAsync<HttpRequestException>(() => PublicAddressGuard.ResolvePublicAsync("mixed.test", Resolve, default));
        await Assert.ThrowsAsync<HttpRequestException>(() => PublicAddressGuard.ResolvePublicAsync("rebind.test", Resolve, default));
        await Assert.ThrowsAsync<HttpRequestException>(() => PublicAddressGuard.ResolvePublicAsync("192.168.0.9", Resolve, default));
    }
}

public sealed class ContentScanProcessorTests : IDisposable
{
    private sealed class FakeFetcher : ISafeHttpFetcher
    {
        public Dictionary<string, (string Body, string Type)> Pages { get; } = [];
        public List<string> Requested { get; } = [];

        public Task<FetchResult> FetchAsync(Uri uri, FetchOptions? options, CancellationToken cancellationToken)
        {
            Requested.Add(uri.AbsoluteUri);
            if (!Pages.TryGetValue(uri.AbsoluteUri, out var page))
            {
                throw new FetchException("HTTP 404", 404);
            }

            return Task.FromResult(new FetchResult(uri, page.Type, Encoding.UTF8.GetBytes(page.Body), null, null, false));
        }

        public void SetMinimumInterval(string host, TimeSpan interval)
        {
        }
    }

    private readonly ApplicationDbContext dbContext = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly FakeFetcher fetcher = new();
    private readonly ContentScanProcessor processor;

    public ContentScanProcessorTests()
    {
        var options = Options.Create(new CrawlerOptions());
        processor = new ContentScanProcessor(
            dbContext, fetcher, new RobotsService(fetcher, options, TimeProvider.System), options, TimeProvider.System,
            NullLogger<ContentScanProcessor>.Instance);
    }

    private static string Rss(params (string Title, string Url)[] items) =>
        "<rss><channel>"
        + string.Concat(items.Select(item => $"<item><title>{item.Title}</title><link>{item.Url}</link><description>About {item.Title}</description></item>"))
        + "</channel></rss>";

    private async Task<ContentSourceScan> RunAsync(ContentSource source)
    {
        if (!dbContext.ContentSources.Any(row => row.Id == source.Id))
        {
            dbContext.ContentSources.Add(source);
        }

        var scan = new ContentSourceScan
        {
            Id = Guid.NewGuid(), SourceId = source.Id, Status = ContentScanStatus.Running,
            StartedAt = DateTimeOffset.UtcNow, RequestedAt = DateTimeOffset.UtcNow,
        };
        dbContext.ContentSourceScans.Add(scan);
        await dbContext.SaveChangesAsync();
        await processor.ProcessAsync(scan.Id, default);
        return await dbContext.ContentSourceScans.AsNoTracking().SingleAsync(row => row.Id == scan.Id);
    }

    private static ContentSource FeedSource(ContentRetentionPolicy retention = ContentRetentionPolicy.ExcerptAndSummary) => new()
    {
        Id = Guid.NewGuid(), Name = "Feed", BaseUrl = "https://example.com", FeedUrl = "https://example.com/feed.xml",
        ExtractionMethod = ContentExtractionMethod.Feed, SourceType = ContentSourceType.RssFeed, RetentionPolicy = retention,
    };

    [Fact]
    public async Task FeedScanRecordsItemsPrivatelyAndIsIdempotent()
    {
        fetcher.Pages["https://example.com/feed.xml"] = (Rss(("One", "https://example.com/1"), ("Two", "https://example.com/2")), "application/rss+xml");
        var source = FeedSource();
        var first = await RunAsync(source);
        Assert.Equal(ContentScanStatus.Succeeded, first.Status);
        Assert.Equal(2, first.DiscoveredCount);
        var row = await dbContext.DiscoveredContents.FirstAsync();
        Assert.Equal(ContentVisibility.Private, row.Visibility);
        Assert.Equal(DiscoveredContentStatus.PendingReview, row.Status);
        Assert.Null(row.TextContent);

        var second = await RunAsync(source);
        Assert.Equal(0, second.DiscoveredCount);
        Assert.Equal(2, second.UnchangedCount);
        Assert.Equal(2, await dbContext.DiscoveredContents.CountAsync());
        Assert.Equal(ContentSourceHealth.Healthy, (await dbContext.ContentSources.FindAsync(source.Id))!.Health);
    }

    [Fact]
    public async Task ChangedContentCreatesANewVersionAndKeepsHistory()
    {
        var source = FeedSource();
        fetcher.Pages["https://example.com/feed.xml"] = (Rss(("Original", "https://example.com/1")), "application/rss+xml");
        await RunAsync(source);
        fetcher.Pages["https://example.com/feed.xml"] = (Rss(("Original updated", "https://example.com/1")), "application/rss+xml");
        var scan = await RunAsync(source);
        Assert.Equal(1, scan.UpdatedCount);
        var row = await dbContext.DiscoveredContents.SingleAsync();
        Assert.Equal(2, row.Version);
        Assert.Equal(2, await dbContext.DiscoveredContentVersions.CountAsync());
    }

    [Fact]
    public async Task MetadataOnlyRetentionKeepsNoExcerptOrText()
    {
        fetcher.Pages["https://example.com/feed.xml"] = (Rss(("One", "https://example.com/1")), "application/rss+xml");
        await RunAsync(FeedSource(ContentRetentionPolicy.LinkAndMetadataOnly));
        var row = await dbContext.DiscoveredContents.SingleAsync();
        Assert.Null(row.Excerpt);
        Assert.Null(row.TextContent);
    }

    [Fact]
    public async Task RobotsDisallowStopsTheScanAndMarksTheSource()
    {
        fetcher.Pages["https://example.com/robots.txt"] = ("User-agent: *\nDisallow: /feed.xml", "text/plain");
        fetcher.Pages["https://example.com/feed.xml"] = (Rss(("One", "https://example.com/1")), "application/rss+xml");
        var source = FeedSource();
        var scan = await RunAsync(source);
        Assert.Equal(ContentScanStatus.Failed, scan.Status);
        Assert.Contains("robots.txt", scan.ErrorMessage);
        Assert.Empty(dbContext.DiscoveredContents);
        Assert.Equal(1, (await dbContext.ContentSources.FindAsync(source.Id))!.ConsecutiveFailures);
    }

    [Fact]
    public async Task ManualOnlyAndFetchFailuresFailTheScanWithoutThrowing()
    {
        var manual = FeedSource();
        manual.ExtractionMethod = ContentExtractionMethod.ManualOnly;
        Assert.Equal(ContentScanStatus.Failed, (await RunAsync(manual)).Status);

        var missing = FeedSource();
        missing.BaseUrl = "https://other.example.org";
        missing.FeedUrl = "https://other.example.org/none.xml";
        Assert.Equal(ContentScanStatus.Failed, (await RunAsync(missing)).Status);
    }

    [Fact]
    public async Task HtmlExtractionFollowsSameSiteLinksHonoursPatternsAndIsolatesFailures()
    {
        fetcher.Pages["https://example.com/"] = (
            "<html><body><a href=\"/news/a\">A</a><a href=\"/news/missing\">M</a><a href=\"/about\">About</a><a href=\"https://elsewhere.org/x\">Out</a></body></html>",
            "text/html");
        fetcher.Pages["https://example.com/news/a"] = (
            "<html lang=\"en\"><head><title>Story A</title></head><body><article>Some long educational text.</article></body></html>",
            "text/html");
        var source = new ContentSource
        {
            Id = Guid.NewGuid(), Name = "Site", BaseUrl = "https://example.com/", ExtractionMethod = ContentExtractionMethod.HtmlExtraction,
            IncludePatterns = "/news/*", RetentionPolicy = ContentRetentionPolicy.FullTextPermitted, RightsNote = "licensed",
        };
        var scan = await RunAsync(source);
        Assert.Equal(ContentScanStatus.PartiallySucceeded, scan.Status);
        Assert.Equal(1, scan.DiscoveredCount);
        Assert.Equal(1, scan.FailedCount);
        var row = await dbContext.DiscoveredContents.SingleAsync();
        Assert.Equal("Story A", row.Title);
        Assert.Equal("Some long educational text.", row.TextContent);
        Assert.DoesNotContain(fetcher.Requested, url => url.Contains("elsewhere") || url.EndsWith("/about"));
    }

    [Fact]
    public async Task SitemapScanKeepsOnlyOwnHostUrls()
    {
        fetcher.Pages["https://example.com/sitemap.xml"] = (
            "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><url><loc>https://example.com/p1</loc></url><url><loc>https://evil.test/p2</loc></url></urlset>",
            "application/xml");
        var source = new ContentSource
        {
            Id = Guid.NewGuid(), Name = "Map", BaseUrl = "https://example.com", SitemapUrl = "https://example.com/sitemap.xml",
            ExtractionMethod = ContentExtractionMethod.Sitemap,
        };
        var scan = await RunAsync(source);
        Assert.Equal(1, scan.DiscoveredCount);
        Assert.Equal("https://example.com/p1", (await dbContext.DiscoveredContents.SingleAsync()).CanonicalUrl);
    }

    [Fact]
    public async Task WorkerClaimsAndRunsQueuedScans()
    {
        fetcher.Pages["https://example.com/feed.xml"] = (Rss(("One", "https://example.com/1")), "application/rss+xml");
        var source = FeedSource();
        dbContext.ContentSources.Add(source);
        dbContext.ContentSourceScans.Add(new ContentSourceScan
        {
            Id = Guid.NewGuid(), SourceId = source.Id, Status = ContentScanStatus.Queued, RequestedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton<ISafeHttpFetcher>(fetcher);
        services.AddSingleton(Options.Create(new CrawlerOptions()));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<RobotsService>();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<ContentScanProcessor>>(NullLogger<ContentScanProcessor>.Instance);
        services.AddScoped<ContentScanProcessor>();
        using var provider = services.BuildServiceProvider();
        var worker = new ContentScanWorker(
            provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(new CrawlerOptions()), TimeProvider.System,
            NullLogger<ContentScanWorker>.Instance);

        Assert.True(await worker.RunNextAsync(default));
        Assert.False(await worker.RunNextAsync(default));
        Assert.Equal(ContentScanStatus.Succeeded, (await dbContext.ContentSourceScans.AsNoTracking().SingleAsync()).Status);
    }

    public void Dispose() => dbContext.Dispose();
}

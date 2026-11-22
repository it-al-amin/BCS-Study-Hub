using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Infrastructure.Persistence;

/// <summary>
/// Seeds a starter list of BCS-related sources so scanning works without typing each URL. Idempotent: a source whose
/// base URL already exists is never touched, so admin edits survive restarts. Every entry was checked on 2026-11-23
/// (robots.txt, page structure); see CRAWLING_POLICY.md. Seeded sources keep a link/excerpt retention policy.
/// </summary>
public sealed class ContentSourceSeeder(ApplicationDbContext dbContext, ILogger<ContentSourceSeeder> logger)
{
    private sealed record Seed(
        string Name,
        string LocalizedName,
        string BaseUrl,
        ContentExtractionMethod Method,
        string Category,
        string Language,
        int Priority,
        int Reliability,
        string? Include,
        string? Exclude,
        string Notes,
        string? SitemapUrl = null,
        bool Enabled = true);

    private const string PortalNote =
        "Public government notices. robots.txt absent on 2026-11-23, so crawling is not restricted; the bot still limits itself to one request per second.";

    private static readonly string[] BpscExclude = ["/pages/officers", "/pages/photo-galleries", "/pages/biographies", "/pages/committees"];

    private static readonly Seed[] Seeds =
    [
        new("BPSC – Notices", "বিপিএসসি – নোটিশ", "https://bpsc.gov.bd/pages/notices", ContentExtractionMethod.HtmlExtraction, "Official", "bn", 1, 5,
            "/pages/notices/", null, "Bangladesh Public Service Commission notices. " + PortalNote),
        new("BPSC – Circulars", "বিপিএসসি – বিজ্ঞপ্তি", "https://bpsc.gov.bd/pages/notification-circulars", ContentExtractionMethod.HtmlExtraction, "Official", "bn", 1, 5,
            "/pages/notification-circulars/", null, "Official BPSC circulars. " + PortalNote),
        new("BPSC – Exams and results", "বিপিএসসি – পরীক্ষা ও ফলাফল", "https://bpsc.gov.bd/pages/psc-exams", ContentExtractionMethod.HtmlExtraction, "Official", "bn", 1, 5,
            "/pages/psc-exams/", null, "Exam schedules, syllabus pages and results. " + PortalNote),
        new("BPSC – Publications", "বিপিএসসি – প্রকাশনা", "https://bpsc.gov.bd/pages/publications", ContentExtractionMethod.HtmlExtraction, "Official", "bn", 2, 5,
            "/pages/publications/", null, "Bulletins, training manuals and annual reports. " + PortalNote),
        new("Public Administration Ministry – Notices", "জনপ্রশাসন মন্ত্রণালয় – নোটিশ", "https://mopa.gov.bd/pages/notices", ContentExtractionMethod.HtmlExtraction, "Official", "bn", 5, 5,
            "/pages/notices/", null, "Cadre and service-related government notices. " + PortalNote),
        new("Cabinet Division – Notices", "মন্ত্রিপরিষদ বিভাগ – নোটিশ", "https://cabinet.gov.bd/pages/notices", ContentExtractionMethod.HtmlExtraction, "Official", "bn", 5, 5,
            "/pages/notices/", null, "Government notices and circulars. " + PortalNote),
        new("BSS News – Sitemap", "বাসস – সাইটম্যাপ", "https://www.bssnews.net", ContentExtractionMethod.Sitemap, "News", "mixed", 20, 4,
            null, null,
            "Bangladesh Sangbad Sangstha news for current affairs. robots.txt allows crawling (Content-Signal: search=yes, ai-train=no); only titles, links and dates are kept and nothing is used for AI training.",
            "https://www.bssnews.net/sitemap.xml"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = (await dbContext.ContentSources.Select(source => source.BaseUrl).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var added = 0;
        foreach (var seed in Seeds.Where(seed => !existing.Contains(seed.BaseUrl)))
        {
            var isSitemap = seed.Method == ContentExtractionMethod.Sitemap;
            dbContext.ContentSources.Add(new ContentSource
            {
                Id = Guid.NewGuid(),
                Name = seed.Name,
                LocalizedName = seed.LocalizedName,
                SourceType = isSitemap ? ContentSourceType.Sitemap : ContentSourceType.Website,
                ExtractionMethod = seed.Method,
                BaseUrl = seed.BaseUrl,
                SitemapUrl = seed.SitemapUrl,
                Category = seed.Category,
                Language = seed.Language,
                Priority = seed.Priority,
                Reliability = seed.Reliability,
                ScanIntervalMinutes = 1440,
                RetentionPolicy = isSitemap ? ContentRetentionPolicy.LinkAndMetadataOnly : ContentRetentionPolicy.ExcerptAndSummary,
                IncludePatterns = seed.Include,
                ExcludePatterns = seed.Include is null ? null : string.Join('\n', BpscExclude),
                RequiresVerification = true,
                Notes = seed.Notes,
                IsEnabled = seed.Enabled,
                CreatedById = Guid.Empty, // System-created.
                CreatedAt = now,
                UpdatedAt = now,
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} content sources.", added);
        }
    }
}

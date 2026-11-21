using System.Net;
using System.Text.RegularExpressions;
using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Infrastructure.Persistence;

/// <summary>
/// Development-only helper. Visits the landing page of each official source already in the directory and fills a
/// missing description from the page's own meta description. It stores metadata only (never page content), obeys
/// robots.txt, uses https only, fetches one page per second and reads at most 256 KB. Run with: dotnet run -- --scrape-sources
/// </summary>
public sealed partial class OfficialSourceMetadataScraper(
    ApplicationDbContext dbContext,
    ILogger<OfficialSourceMetadataScraper> logger)
{
    private const int MaxBytes = 256 * 1024;
    private static readonly string UserAgent = "BCSStudyHubBot/1.0 (educational directory; metadata only)";

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 3 })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        var sources = await dbContext.OfficialSources
            .Where(source => source.Description == null || source.Description == string.Empty)
            .ToListAsync(cancellationToken);
        var updated = 0;
        foreach (var source in sources)
        {
            if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            try
            {
                if (!await IsAllowedByRobotsAsync(client, uri, cancellationToken))
                {
                    logger.LogInformation("Skipping {Url}: disallowed by robots.txt.", source.Url);
                    continue;
                }

                var html = await ReadLimitedAsync(client, uri, cancellationToken);
                var description = html is null ? null : ExtractDescription(html);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    source.Description = description.Length > 1000 ? description[..1000] : description;
                    source.UpdatedAt = DateTimeOffset.UtcNow;
                    updated++;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning("Could not read {Url}: {Message}", source.Url, ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Filled {Count} source descriptions.", updated);
        return updated;
    }

    private static async Task<bool> IsAllowedByRobotsAsync(HttpClient client, Uri uri, CancellationToken ct)
    {
        try
        {
            var robots = await client.GetAsync(new Uri(uri, "/robots.txt"), ct);
            if (robots.StatusCode != HttpStatusCode.OK)
            {
                return true;
            }

            var applies = false;
            foreach (var raw in (await robots.Content.ReadAsStringAsync(ct)).Split('\n'))
            {
                var line = raw.Split('#')[0].Trim();
                if (line.StartsWith("user-agent:", StringComparison.OrdinalIgnoreCase))
                {
                    applies = line[11..].Trim() == "*";
                }
                else if (applies && line.StartsWith("disallow:", StringComparison.OrdinalIgnoreCase))
                {
                    var rule = line[9..].Trim();
                    if (rule.Length > 0 && uri.AbsolutePath.StartsWith(rule, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (HttpRequestException)
        {
            return true;
        }
    }

    private static async Task<string?> ReadLimitedAsync(HttpClient client, Uri uri, CancellationToken ct)
    {
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "text/html")
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[MaxBytes];
        var total = 0;
        int read;
        while (total < MaxBytes && (read = await stream.ReadAsync(buffer.AsMemory(total, MaxBytes - total), ct)) > 0)
        {
            total += read;
        }

        return System.Text.Encoding.UTF8.GetString(buffer, 0, total);
    }

    private static string? ExtractDescription(string html)
    {
        var match = MetaDescription().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }

    [GeneratedRegex("<meta[^>]+name=[\"']description[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescription();
}

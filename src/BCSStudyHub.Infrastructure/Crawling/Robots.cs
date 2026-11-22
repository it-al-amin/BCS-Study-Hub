using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace BCSStudyHub.Infrastructure.Crawling;

/// <summary>A parsed robots.txt for one user-agent. Longest matching rule wins; Allow wins a tie.</summary>
internal sealed class RobotsRules
{
    private sealed record Rule(Regex Pattern, int Length, bool Allow);

    private readonly List<Rule> rules = [];

    public static RobotsRules AllowAll { get; } = new();
    public static RobotsRules DisallowAll { get; } = Parse("User-agent: *\nDisallow: /", "x");

    public TimeSpan? CrawlDelay { get; private set; }

    public static RobotsRules Parse(string text, string agentToken)
    {
        var groups = new List<(List<string> Agents, List<(bool Allow, string Path)> Rules, double? Delay)>();
        List<string>? agents = null;
        List<(bool, string)>? groupRules = null;
        double? delay = null;
        var lastWasAgent = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim().ToLowerInvariant();
            var value = line[(colon + 1)..].Trim();
            if (key == "user-agent")
            {
                if (!lastWasAgent || agents is null)
                {
                    agents = [];
                    groupRules = [];
                    groups.Add((agents, groupRules, null));
                    delay = null;
                }

                agents.Add(value.ToLowerInvariant());
                lastWasAgent = true;
                continue;
            }

            lastWasAgent = false;
            if (agents is null || groupRules is null)
            {
                continue;
            }

            switch (key)
            {
                case "allow":
                case "disallow":
                    groupRules.Add((key == "allow", value));
                    break;
                case "crawl-delay" when double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds):
                    delay = seconds;
                    groups[^1] = (agents, groupRules, delay);
                    break;
            }
        }

        var token = agentToken.ToLowerInvariant();
        var chosen = groups.Where(group => group.Agents.Any(agent => agent != "*" && token.Contains(agent))).ToList();
        if (chosen.Count == 0)
        {
            chosen = groups.Where(group => group.Agents.Contains("*")).ToList();
        }

        var result = new RobotsRules();
        foreach (var group in chosen)
        {
            foreach (var (allow, path) in group.Rules)
            {
                if (path.Length == 0)
                {
                    continue; // "Disallow:" with no value allows everything; an empty Allow adds nothing.
                }

                result.rules.Add(new Rule(ToRegex(path), path.Length, allow));
            }

            if (group.Delay is double seconds && seconds > 0)
            {
                result.CrawlDelay = TimeSpan.FromSeconds(seconds);
            }
        }

        return result;
    }

    public bool IsAllowed(string pathAndQuery)
    {
        Rule? best = null;
        foreach (var rule in rules)
        {
            if (!rule.Pattern.IsMatch(pathAndQuery))
            {
                continue;
            }

            if (best is null || rule.Length > best.Length || (rule.Length == best.Length && rule.Allow && !best.Allow))
            {
                best = rule;
            }
        }

        return best?.Allow ?? true;
    }

    private static Regex ToRegex(string pattern)
    {
        var builder = new StringBuilder("^");
        foreach (var character in pattern)
        {
            builder.Append(character switch
            {
                '*' => ".*",
                '$' => "$",
                _ => Regex.Escape(character.ToString()),
            });
        }

        return new Regex(builder.ToString(), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
    }
}

/// <summary>Fetches and caches robots.txt per host. If it cannot be read reliably the host is treated as off limits.</summary>
internal sealed class RobotsService(ISafeHttpFetcher fetcher, IOptions<CrawlerOptions> options, TimeProvider timeProvider)
{
    private sealed record Entry(RobotsRules Rules, DateTimeOffset ExpiresAt);

    private static readonly string[] TextTypes = ["text/plain", "text/html", "application/octet-stream"];
    private readonly ConcurrentDictionary<string, Entry> cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsAllowedAsync(Uri uri, CancellationToken cancellationToken)
    {
        var rules = await GetRulesAsync(uri, cancellationToken);
        return rules.IsAllowed(uri.PathAndQuery);
    }

    public async Task<RobotsRules> GetRulesAsync(Uri uri, CancellationToken cancellationToken)
    {
        var host = uri.IdnHost;
        var now = timeProvider.GetUtcNow();
        if (cache.TryGetValue(host, out var cached) && cached.ExpiresAt > now)
        {
            return cached.Rules;
        }

        RobotsRules rules;
        var ttl = TimeSpan.FromHours(1);
        try
        {
            var result = await fetcher.FetchAsync(
                new Uri($"https://{host}/robots.txt"), new FetchOptions(AcceptedContentTypes: TextTypes), cancellationToken);
            rules = RobotsRules.Parse(Encoding.UTF8.GetString(result.Body), options.Value.RobotsToken);
        }
        catch (FetchException exception) when (exception.StatusCode is >= 400 and < 500 and not 429)
        {
            rules = RobotsRules.AllowAll; // No robots.txt: everything is allowed.
        }
        catch (FetchException)
        {
            rules = RobotsRules.DisallowAll; // Server error or network failure: stay away until it recovers.
            ttl = TimeSpan.FromMinutes(10);
        }

        if (rules.CrawlDelay is { } delay)
        {
            var capped = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds, options.Value.MaxCrawlDelaySeconds));
            fetcher.SetMinimumInterval(host, capped);
        }

        cache[host] = new Entry(rules, now + ttl);
        return rules;
    }
}

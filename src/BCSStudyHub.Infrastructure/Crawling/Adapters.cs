using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Infrastructure.Crawling;

internal sealed record DiscoveredItem(
    Uri Url,
    string? Title,
    string? Summary,
    DateTimeOffset? PublishedAt,
    string? Language,
    string? Text,
    DiscoveredContentKind Kind);

internal static class UrlCanonicalizer
{
    private static readonly string[] TrackingPrefixes = ["utm_", "fbclid", "gclid", "mc_", "ref_", "_ga"];

    public static string Canonicalize(Uri uri)
    {
        var path = uri.AbsolutePath.Length > 1 ? uri.AbsolutePath.TrimEnd('/') : uri.AbsolutePath;
        var builder = new UriBuilder(uri) { Fragment = string.Empty, Host = uri.IdnHost.ToLowerInvariant(), Path = path };
        if (uri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        var query = (uri.Query.TrimStart('?')).Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(pair => !TrackingPrefixes.Any(prefix => pair.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(pair => pair, StringComparer.Ordinal);
        builder.Query = string.Join('&', query);
        return builder.Uri.AbsoluteUri;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

internal static class UrlPatternFilter
{
    /// <summary>Patterns are one per line; '*' is a wildcard; matching is case-insensitive on the path and query.</summary>
    public static bool Matches(Uri url, string? includePatterns, string? excludePatterns)
    {
        var target = url.PathAndQuery;
        if (Split(excludePatterns).Any(pattern => IsMatch(target, pattern)))
        {
            return false;
        }

        var includes = Split(includePatterns).ToList();
        return includes.Count == 0 || includes.Any(pattern => IsMatch(target, pattern));
    }

    private static IEnumerable<string> Split(string? patterns) =>
        (patterns ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsMatch(string target, string pattern)
    {
        var regex = "^.*" + Regex.Escape(pattern).Replace("\\*", ".*") + ".*$";
        return Regex.IsMatch(target, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
    }
}

internal static partial class HtmlText
{
    public static string ToPlainText(string html)
    {
        var withoutNoise = NoiseBlocks().Replace(Comments().Replace(html, " "), " ");
        var main = MainBlock().Match(withoutNoise);
        var text = Tags().Replace(main.Success ? main.Groups[2].Value : withoutNoise, " ");
        return Normalize(WebUtility.HtmlDecode(text));
    }

    public static string StripTags(string html) => Normalize(WebUtility.HtmlDecode(Tags().Replace(html, " ")));

    public static string Normalize(string text) => Whitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comments();

    [GeneratedRegex(@"<(script|style|noscript|svg|nav|header|footer|aside|form|iframe)\b.*?</\1\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex NoiseBlocks();

    [GeneratedRegex(@"<(article|main)\b[^>]*>(.*?)</\1\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex MainBlock();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

/// <summary>RSS 2.0, RSS 1.0 and Atom. XML is parsed with DTDs and external resolvers disabled.</summary>
internal static class FeedParser
{
    public static XDocument LoadXml(byte[] body)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4_000_000 };
        using var reader = XmlReader.Create(new MemoryStream(body), settings);
        return XDocument.Load(reader);
    }

    public static IReadOnlyList<DiscoveredItem> Parse(byte[] body, Uri baseUri, string? defaultLanguage)
    {
        var document = LoadXml(body);
        var items = new List<DiscoveredItem>();
        foreach (var node in document.Descendants().Where(element => element.Name.LocalName is "item" or "entry"))
        {
            var link = LinkOf(node);
            if (link is null || !Uri.TryCreate(baseUri, link, out var url) || url.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            var title = Text(node, "title");
            var summary = Text(node, "description") ?? Text(node, "summary") ?? Text(node, "content");
            var published = Date(Text(node, "pubDate") ?? Text(node, "published") ?? Text(node, "updated") ?? Text(node, "date"));
            items.Add(new DiscoveredItem(
                url,
                title is null ? null : HtmlText.StripTags(title),
                summary is null ? null : HtmlText.StripTags(summary),
                published,
                defaultLanguage,
                null,
                DiscoveredContentKind.Article));
        }

        return items;
    }

    private static string? LinkOf(XElement node)
    {
        var links = node.Elements().Where(element => element.Name.LocalName == "link").ToList();
        var atom = links.FirstOrDefault(link => link.Attribute("href") is not null
                                                && ((string?)link.Attribute("rel") is null or "alternate"));
        return atom is not null ? (string?)atom.Attribute("href") : links.FirstOrDefault()?.Value.Trim();
    }

    private static string? Text(XElement node, string name) =>
        node.Elements().FirstOrDefault(element => element.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value is { Length: > 0 } value
            ? value.Trim()
            : null;

    private static DateTimeOffset? Date(string? value) =>
        DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
}

internal static class SitemapParser
{
    public sealed record Entry(Uri Location, DateTimeOffset? LastModified);

    public sealed record Result(IReadOnlyList<Entry> Urls, IReadOnlyList<Uri> ChildSitemaps);

    public static Result Parse(byte[] body)
    {
        var document = FeedParser.LoadXml(body);
        var urls = new List<Entry>();
        var children = new List<Uri>();
        foreach (var node in document.Descendants())
        {
            var isUrl = node.Name.LocalName == "url";
            var isSitemap = node.Name.LocalName == "sitemap";
            if (!isUrl && !isSitemap)
            {
                continue;
            }

            var loc = node.Elements().FirstOrDefault(element => element.Name.LocalName == "loc")?.Value.Trim();
            if (loc is null || !Uri.TryCreate(loc, UriKind.Absolute, out var location) || location.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            if (isSitemap)
            {
                children.Add(location);
                continue;
            }

            var lastmod = node.Elements().FirstOrDefault(element => element.Name.LocalName == "lastmod")?.Value;
            urls.Add(new Entry(location, DateTimeOffset.TryParse(lastmod, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null));
        }

        return new Result(urls, children);
    }
}

internal static partial class HtmlPage
{
    private static readonly string[] SkippedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp", ".ico", ".css", ".js", ".zip", ".rar", ".mp3", ".mp4", ".avi", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];

    public sealed record Extracted(string? Title, string? Description, string? Canonical, string? Language, DateTimeOffset? Published, string Text, IReadOnlyList<Uri> Links);

    public static Extracted Extract(string html, Uri pageUrl)
    {
        var titleTag = TitleTag().Match(html);
        var title = Meta(html, "og:title") ?? (titleTag.Success ? HtmlText.StripTags(titleTag.Groups[1].Value) : null);
        var description = Meta(html, "description") ?? Meta(html, "og:description");
        var canonical = CanonicalLink().Match(html) is { Success: true } canon ? canon.Groups[1].Value : null;
        var language = HtmlLang().Match(html) is { Success: true } lang ? lang.Groups[1].Value.ToLowerInvariant() : null;
        var published = DateTimeOffset.TryParse(
            Meta(html, "article:published_time") ?? Meta(html, "date"),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsed) ? parsed : (DateTimeOffset?)null;

        var links = new List<Uri>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match anchor in Anchors().Matches(html))
        {
            var href = WebUtility.HtmlDecode(anchor.Groups[1].Value).Trim();
            if (href.Length == 0 || href.StartsWith('#') || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || !Uri.TryCreate(pageUrl, href, out var target)
                || target.Scheme != Uri.UriSchemeHttps
                || SkippedExtensions.Any(extension => target.AbsolutePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (seen.Add(UrlCanonicalizer.Canonicalize(target)))
            {
                links.Add(target);
            }
        }

        return new Extracted(title, description, canonical, language, published, HtmlText.ToPlainText(html), links);
    }

    private static string? Meta(string html, string name)
    {
        foreach (Match tag in MetaTags().Matches(html))
        {
            var text = tag.Value;
            var key = Attribute(text, "name") ?? Attribute(text, "property");
            if (key is not null && key.Equals(name, StringComparison.OrdinalIgnoreCase) && Attribute(text, "content") is { Length: > 0 } content)
            {
                return WebUtility.HtmlDecode(content).Trim();
            }
        }

        return null;
    }

    private static string? Attribute(string tag, string name)
    {
        var match = Regex.Match(tag, $@"\b{name}\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
        return match.Success ? (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value) : null;
    }

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex TitleTag();

    [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex MetaTags();

    [GeneratedRegex(@"<link\b[^>]*rel\s*=\s*[""']canonical[""'][^>]*href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex CanonicalLink();

    [GeneratedRegex(@"<html\b[^>]*\blang\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlLang();

    [GeneratedRegex(@"<a\b[^>]*\bhref\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex Anchors();
}

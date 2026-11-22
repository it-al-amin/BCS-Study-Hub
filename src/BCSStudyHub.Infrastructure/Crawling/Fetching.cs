using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BCSStudyHub.Application.ContentSources;
using Microsoft.Extensions.Options;

namespace BCSStudyHub.Infrastructure.Crawling;

public sealed class CrawlerOptions
{
    public const string SectionName = "Crawler";

    public string UserAgent { get; set; } = "BCSStudyHubBot/1.0 (+private educational study tool)";
    public string RobotsToken { get; set; } = "BCSStudyHubBot";
    public int MinHostIntervalMilliseconds { get; set; } = 1000;
    public int MaxBytes { get; set; } = 2 * 1024 * 1024;
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxRedirects { get; set; } = 3;
    public int MaxItemsPerScan { get; set; } = 200;
    public int MaxPagesFetchedPerScan { get; set; } = 20;
    public int MaxCrawlDelaySeconds { get; set; } = 30;
    public bool WorkerEnabled { get; set; } = true;
    public int WorkerPollSeconds { get; set; } = 10;
}

internal sealed record FetchOptions(string? ETag = null, DateTimeOffset? IfModifiedSince = null, string[]? AcceptedContentTypes = null);

internal sealed record FetchResult(
    Uri FinalUri,
    string? ContentType,
    byte[] Body,
    string? ETag,
    DateTimeOffset? LastModified,
    bool NotModified);

internal sealed class FetchException(string message, int? statusCode = null, bool transient = false) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
    public bool Transient { get; } = transient;
}

internal interface ISafeHttpFetcher
{
    Task<FetchResult> FetchAsync(Uri uri, FetchOptions? options, CancellationToken cancellationToken);
    void SetMinimumInterval(string host, TimeSpan interval);
}

/// <summary>Decides whether a host name resolves only to public addresses. Used on every connection to stop DNS rebinding.</summary>
internal static class PublicAddressGuard
{
    public static async Task<IPAddress[]> ResolvePublicAsync(
        string host, Func<string, CancellationToken, Task<IPAddress[]>> resolve, CancellationToken cancellationToken)
    {
        var addresses = IPAddress.TryParse(host, out var literal) ? [literal] : await resolve(host, cancellationToken);
        if (addresses.Length == 0)
        {
            throw new HttpRequestException($"Host '{host}' did not resolve.");
        }

        // Every address must be public: a mixed answer is how rebinding attacks smuggle in an internal address.
        if (addresses.Any(address => !SourceUrlRules.IsPublic(address)))
        {
            throw new HttpRequestException($"Host '{host}' resolves to a non-public address and was blocked.");
        }

        return addresses;
    }
}

/// <summary>Spaces requests to the same host so a scan never hammers a site.</summary>
internal sealed class HostRateLimiter(TimeSpan defaultInterval, TimeProvider timeProvider)
{
    private sealed class State
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public DateTimeOffset Next = DateTimeOffset.MinValue;
        public TimeSpan Interval;
    }

    private readonly ConcurrentDictionary<string, State> hosts = new(StringComparer.OrdinalIgnoreCase);

    private State For(string host) => hosts.GetOrAdd(host, _ => new State { Interval = defaultInterval });

    public void SetInterval(string host, TimeSpan interval)
    {
        var state = For(host);
        state.Interval = interval > defaultInterval ? interval : defaultInterval;
    }

    public async Task WaitAsync(string host, CancellationToken cancellationToken)
    {
        var state = For(host);
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            var wait = state.Next - timeProvider.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, timeProvider, cancellationToken);
            }

            state.Next = timeProvider.GetUtcNow() + state.Interval;
        }
        finally
        {
            state.Gate.Release();
        }
    }
}

/// <summary>
/// The only component that talks to the internet. It validates every URL (including each redirect), connects only to
/// public IP addresses, follows a bounded number of redirects, caps response size and time, and rate-limits per host.
/// </summary>
internal sealed class SafeHttpFetcher : ISafeHttpFetcher, IDisposable
{
    private static readonly string[] DefaultContentTypes =
        ["text/html", "application/xhtml+xml", "application/xml", "text/xml", "application/rss+xml", "application/atom+xml", "text/plain"];

    private readonly HttpClient client;
    private readonly CrawlerOptions options;
    private readonly HostRateLimiter limiter;

    public SafeHttpFetcher(IOptions<CrawlerOptions> options, TimeProvider timeProvider, HttpMessageHandler? handler = null)
    {
        this.options = options.Value;
        limiter = new HostRateLimiter(TimeSpan.FromMilliseconds(this.options.MinHostIntervalMilliseconds), timeProvider);
        client = new HttpClient(handler ?? CreateHandler(), disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(this.options.TimeoutSeconds),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(this.options.UserAgent);
    }

    public void SetMinimumInterval(string host, TimeSpan interval) => limiter.SetInterval(host, interval);

    public async Task<FetchResult> FetchAsync(Uri uri, FetchOptions? fetchOptions, CancellationToken cancellationToken)
    {
        var accepted = fetchOptions?.AcceptedContentTypes ?? DefaultContentTypes;
        for (var redirects = 0; ; redirects++)
        {
            if (SourceUrlRules.Validate(uri.AbsoluteUri, true, "URL") is { } problem)
            {
                throw new FetchException(problem);
            }

            await limiter.WaitAsync(uri.IdnHost, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(fetchOptions?.ETag))
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", fetchOptions.ETag);
            }

            if (fetchOptions?.IfModifiedSince is { } since)
            {
                request.Headers.IfModifiedSince = since;
            }

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new FetchException(exception.Message, null, transient: true);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new FetchException("The request timed out.", null, transient: true);
            }

            using (response)
            {
                var status = (int)response.StatusCode;
                if (status is 301 or 302 or 303 or 307 or 308)
                {
                    if (redirects >= options.MaxRedirects || response.Headers.Location is null)
                    {
                        throw new FetchException("Too many redirects.", status);
                    }

                    uri = new Uri(uri, response.Headers.Location);
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    return new FetchResult(uri, null, [], fetchOptions?.ETag, fetchOptions?.IfModifiedSince, true);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new FetchException($"HTTP {status}", status, transient: status == 429 || status >= 500);
                }

                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                if (contentType is null || !accepted.Contains(contentType))
                {
                    throw new FetchException($"Content type '{contentType ?? "unknown"}' is not accepted.");
                }

                if (response.Content.Headers.ContentLength > options.MaxBytes)
                {
                    throw new FetchException("The response is larger than the allowed size.");
                }

                var body = await ReadCappedAsync(response, cancellationToken);
                return new FetchResult(
                    uri, contentType, body, response.Headers.ETag?.ToString(), response.Content.Headers.LastModified, false);
            }
        }
    }

    private async Task<byte[]> ReadCappedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > options.MaxBytes)
            {
                throw new FetchException("The response is larger than the allowed size.");
            }
        }

        return buffer.ToArray();
    }

    private static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        UseProxy = false,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        ConnectCallback = ConnectToPublicAddressAsync,
    };

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await PublicAddressGuard.ResolvePublicAsync(
            context.DnsEndPoint.Host, (host, token) => Dns.GetHostAddressesAsync(host, token), cancellationToken);
        Exception? last = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception exception)
            {
                socket.Dispose();
                last = exception;
            }
        }

        throw new HttpRequestException("Could not connect to the host.", last);
    }

    public void Dispose() => client.Dispose();
}

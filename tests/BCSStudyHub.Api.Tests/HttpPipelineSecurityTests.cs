using System.Net;
using BCSStudyHub.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Api.Tests;

/// <summary>
/// Exercises the real security middleware and rate-limit configuration through an in-memory HTTP server.
/// (The full API cannot start here because it needs MySQL; these tests cover the pipeline pieces independently.)
/// </summary>
public sealed class HttpPipelineSecurityTests
{
    private static async Task<IHost> StartAsync(Action<WebApplication> map)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddRateLimiter(SecurityLimits.ConfigureRateLimiter);
        var app = builder.Build();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRateLimiter();
        map(app);
        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task ResponsesCarrySecurityHeaders()
    {
        using var host = await StartAsync(app => app.MapGet("/ping", () => "ok"));
        var response = await host.GetTestClient().GetAsync("/ping");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task EndpointsCanKeepTheirOwnStricterPolicy()
    {
        using var host = await StartAsync(app => app.MapGet("/file", (HttpContext context) =>
        {
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return "file";
        }));
        var response = await host.GetTestClient().GetAsync("/file");

        Assert.Equal("default-src 'none'; sandbox", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task AuthEndpointsAreRateLimitedPerClient()
    {
        using var host = await StartAsync(app => app.MapPost("/login", () => Results.Ok()).RequireRateLimiting(SecurityLimits.AuthPolicy));
        var client = host.GetTestClient();
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < SecurityLimits.AuthPermitPerMinute + 2; attempt++)
        {
            statuses.Add((await client.PostAsync("/login", null)).StatusCode);
        }

        Assert.Equal(SecurityLimits.AuthPermitPerMinute, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.All(statuses.Skip(SecurityLimits.AuthPermitPerMinute), status => Assert.Equal(HttpStatusCode.TooManyRequests, status));
    }

    [Fact]
    public async Task UnhandledErrorsDoNotLeakDetails()
    {
        using var host = await StartAsync(app => app.MapGet("/boom", (HttpContext _) => throw new InvalidOperationException("Connection string Server=secret-host;Password=hunter2")));
        var response = await host.GetTestClient().GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("hunter2", body);
        Assert.DoesNotContain("secret-host", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }
}

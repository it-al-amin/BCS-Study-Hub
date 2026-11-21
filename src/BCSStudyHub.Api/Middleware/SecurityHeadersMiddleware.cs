namespace BCSStudyHub.Api.Middleware;

/// <summary>
/// Baseline security headers for every API response. The API only returns JSON and files, so the default CSP forbids
/// everything; endpoints that set their own (for example library file downloads) keep their stricter values.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers.TryAdd("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
            }

            return Task.CompletedTask;
        });
        return next(context);
    }
}

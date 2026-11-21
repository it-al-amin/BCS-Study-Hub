using System.Security.Claims;
using System.Threading.RateLimiting;
using BCSStudyHub.Api.Controllers;
using Microsoft.AspNetCore.RateLimiting;

namespace BCSStudyHub.Api.Middleware;

public static class SecurityLimits
{
    public const string AuthPolicy = "auth";
    public const int AuthPermitPerMinute = 10;
    public const int UploadPermitPerWindow = 30;
    public static readonly TimeSpan UploadWindow = TimeSpan.FromMinutes(10);

    /// <summary>Shared by Program and the tests so the verified limits are the deployed limits.</summary>
    public static void ConfigureRateLimiter(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Uploads: per signed-in user (falling back to IP).
        options.AddPolicy(LibraryRateLimits.UploadPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = UploadPermitPerWindow, Window = UploadWindow, QueueLimit = 0 }));

        // Login and registration: per client IP, to slow credential stuffing and sign-up abuse.
        // Per-account lockout (Identity) still applies on top of this.
        options.AddPolicy(AuthPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = AuthPermitPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    }
}

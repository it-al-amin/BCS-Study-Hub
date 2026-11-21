using BCSStudyHub.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BCSStudyHub.Api.Authorization;

/// <summary>
/// Registers one authorization policy per permission code, each requiring the matching "permission" claim from the
/// JWT. Driving this from <see cref="PermissionCodes.Descriptions"/> means a new permission can never be left without
/// a server-side policy.
/// </summary>
public static class PermissionPolicies
{
    public const string ClaimType = "permission";

    public static void Register(AuthorizationOptions options)
    {
        foreach (var code in PermissionCodes.Descriptions.Keys)
        {
            options.AddPolicy(code, policy => policy.RequireAuthenticatedUser().RequireClaim(ClaimType, code));
        }
    }
}

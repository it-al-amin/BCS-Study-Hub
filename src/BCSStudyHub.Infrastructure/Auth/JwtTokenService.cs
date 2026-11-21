using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCSStudyHub.Infrastructure.Configuration;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BCSStudyHub.Infrastructure.Auth;

internal sealed class JwtTokenService(JwtSettings settings)
{
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

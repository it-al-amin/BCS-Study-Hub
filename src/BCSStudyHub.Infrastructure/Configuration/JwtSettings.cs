using System.Text;

namespace BCSStudyHub.Infrastructure.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) ||
            string.IsNullOrWhiteSpace(Audience) ||
            Encoding.UTF8.GetByteCount(SigningKey) < 32 ||
            AccessTokenMinutes is < 5 or > 60)
        {
            throw new InvalidOperationException(
                "JWT configuration is invalid. Configure issuer, audience, a signing key of at least 32 bytes, and an access-token lifetime between 5 and 60 minutes.");
        }
    }
}

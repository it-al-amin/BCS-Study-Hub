namespace BCSStudyHub.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUser(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUser User);

public enum AuthFailure
{
    None,
    InvalidCredentials,
    EmailAlreadyRegistered,
    RegistrationRejected,
    RegistrationClosed,
}

public sealed record AuthOperationResult(AuthResponse? Response, AuthFailure Failure)
{
    public bool Succeeded => Response is not null && Failure == AuthFailure.None;

    public static AuthOperationResult Success(AuthResponse response) => new(response, AuthFailure.None);

    public static AuthOperationResult Failed(AuthFailure failure) => new(null, failure);
}

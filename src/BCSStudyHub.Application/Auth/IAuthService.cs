namespace BCSStudyHub.Application.Auth;

public interface IAuthService
{
    Task<AuthOperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}

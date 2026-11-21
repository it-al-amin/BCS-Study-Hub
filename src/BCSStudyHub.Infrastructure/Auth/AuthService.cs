using BCSStudyHub.Application.Auth;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Auth;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext dbContext,
    JwtTokenService tokenService,
    IHttpContextAccessor httpContextAccessor,
    BCSStudyHub.Application.Admin.ISiteSettingsService siteSettings)
    : IAuthService
{
    public async Task<AuthOperationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Enforced here, not only hidden in the UI, so a closed registration cannot be bypassed by calling the API.
        if (!await siteSettings.IsRegistrationOpenAsync(cancellationToken))
        {
            return AuthOperationResult.Failed(AuthFailure.RegistrationClosed);
        }

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return AuthOperationResult.Failed(AuthFailure.EmailAlreadyRegistered);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = false,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AuthOperationResult.Failed(
                createResult.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName")
                    ? AuthFailure.EmailAlreadyRegistered
                    : AuthFailure.RegistrationRejected);
        }

        if (await roleManager.FindByNameAsync("Candidate") is null)
        {
            throw new InvalidOperationException("The Candidate role has not been seeded.");
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Candidate");
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException("The new account could not be assigned its default role.");
        }

        AddAuditLog(user.Id, "auth.registered", nameof(ApplicationUser), user.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthOperationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return AuthOperationResult.Failed(AuthFailure.InvalidCredentials);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return AuthOperationResult.Failed(AuthFailure.InvalidCredentials);
        }

        AddAuditLog(user.Id, "auth.login.succeeded", nameof(ApplicationUser), user.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var permissions = await GetPermissionsAsync(user.Id, cancellationToken);
        return new AuthUser(user.Id, user.Email!, user.DisplayName, roles, permissions);
    }

    private async Task<AuthOperationResult> CreateAuthResultAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var permissions = await GetPermissionsAsync(user.Id, cancellationToken);
        var (token, expiresAt) = tokenService.CreateToken(user, roles, permissions);
        var authUser = new AuthUser(user.Id, user.Email!, user.DisplayName, roles, permissions);

        return AuthOperationResult.Success(new AuthResponse(token, expiresAt, authUser));
    }

    private async Task<string[]> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await dbContext.UserRoles
            .Where(mapping => mapping.UserId == userId)
            .Select(mapping => mapping.RoleId)
            .ToListAsync(cancellationToken);
        return await dbContext.RolePermissions
            .Where(mapping => roleIds.Contains(mapping.RoleId))
            .Select(mapping => mapping.Permission.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    private void AddAuditLog(Guid? actorId, string action, string? entityType, string? entityId)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier,
        });
    }
}

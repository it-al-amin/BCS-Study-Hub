using BCSStudyHub.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCSStudyHub.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting(SecurityLimits.AuthPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestBody request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(
            new RegisterRequest(request.Email, request.Password, request.DisplayName),
            cancellationToken);

        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(Me), null, result.Response);
        }

        return result.Failure switch
        {
            AuthFailure.EmailAlreadyRegistered => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Registration conflict",
                Detail = "An account with this email already exists.",
            }),
            AuthFailure.RegistrationClosed => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Registration closed",
                Detail = "New registrations are currently closed by the site administrator.",
            }),
            _ => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Registration rejected",
                Detail = "The account could not be created. Check the password requirements and try again.",
            }),
        };
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(SecurityLimits.AuthPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestBody request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new LoginRequest(request.Email, request.Password),
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Response)
            : Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "The email or password is incorrect.",
            });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthUser>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetUserAsync(userId, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }
}

public sealed class RegisterRequestBody
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class LoginRequestBody
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}

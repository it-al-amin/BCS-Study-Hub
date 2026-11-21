using System.Security.Claims;
using BCSStudyHub.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = PermissionCodes.AdminAccess)]
public sealed class AdminController : ControllerBase
{
    [HttpGet("access")]
    public IActionResult GetAccess()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value),
        });
    }
}

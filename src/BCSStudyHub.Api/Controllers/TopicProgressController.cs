using System.Security.Claims;
using BCSStudyHub.Application.Syllabus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/me/topic-progress")]
[Authorize]
public sealed class TopicProgressController(ISyllabusService syllabusService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TopicProgressDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid versionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await syllabusService.GetProgressAsync(userId, versionId, cancellationToken));
    }

    [HttpPut("{syllabusItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Set(
        Guid syllabusItemId,
        [FromBody] SetTopicProgressRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await syllabusService.SetProgressAsync(
            userId,
            syllabusItemId,
            request.IsCompleted,
            cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}

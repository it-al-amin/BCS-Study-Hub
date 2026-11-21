using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Syllabus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/syllabus")]
[AllowAnonymous]
public sealed class SyllabusController(ISyllabusService syllabusService) : ControllerBase
{
    [HttpGet("stages")]
    [ProducesResponseType<IReadOnlyList<ExamStageDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStages(CancellationToken cancellationToken) =>
        Ok(await syllabusService.GetStagesAsync(cancellationToken));

    [HttpGet("versions")]
    [ProducesResponseType<PagedResponse<SyllabusVersionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions(
        [FromQuery] string? stageCode,
        [FromQuery] string? trackCode,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "stage",
        CancellationToken cancellationToken = default)
    {
        var result = await syllabusService.GetVersionsAsync(
            new SyllabusVersionQuery(stageCode, trackCode, null, pageNumber, pageSize, sortBy),
            includeUnpublished: false,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("versions/{versionId:guid}/tree")]
    [ProducesResponseType<SyllabusTreeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTree(Guid versionId, CancellationToken cancellationToken)
    {
        var tree = await syllabusService.GetTreeAsync(
            versionId,
            TryGetUserId(),
            includeUnpublished: false,
            cancellationToken);
        return tree is null ? NotFound() : Ok(tree);
    }

    [HttpGet("versions/{versionId:guid}/subjects")]
    public async Task<IActionResult> GetVersionSubjects(Guid versionId, CancellationToken cancellationToken)
    {
        var tree = await syllabusService.GetTreeAsync(
            versionId,
            TryGetUserId(),
            includeUnpublished: false,
            cancellationToken);
        return tree is null ? NotFound() : Ok(tree.Subjects);
    }

    [HttpGet("papers")]
    public async Task<IActionResult> GetPapers(
        [FromQuery, Required] Guid syllabusSubjectId,
        CancellationToken cancellationToken)
    {
        var papers = await syllabusService.GetPapersAsync(
            syllabusSubjectId,
            includeUnpublished: false,
            cancellationToken);
        return Ok(papers);
    }

    [HttpGet("items")]
    [HttpGet("topics")]
    [HttpGet("subtopics")]
    [ProducesResponseType<PagedResponse<SyllabusItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems(
        [FromQuery, Required] Guid versionId,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? parentId,
        [FromQuery] string? kind,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (Request.Path.Value?.EndsWith("/topics", StringComparison.OrdinalIgnoreCase) == true)
        {
            kind = nameof(BCSStudyHub.Domain.Entities.SyllabusItemKind.Topic);
        }
        else if (Request.Path.Value?.EndsWith("/subtopics", StringComparison.OrdinalIgnoreCase) == true)
        {
            kind = nameof(BCSStudyHub.Domain.Entities.SyllabusItemKind.Subtopic);
        }

        var result = await syllabusService.GetItemsAsync(
            new SyllabusItemQuery(versionId, subjectId, parentId, kind, pageNumber, pageSize),
            includeUnpublished: false,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    [ProducesResponseType<PagedResponse<SyllabusSearchHitDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery, Required, StringLength(100, MinimumLength = 2)] string q,
        [FromQuery] string? stageCode,
        [FromQuery] Guid? versionId,
        [FromQuery] Guid? subjectId,
        [FromQuery] string? kind,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "title",
        CancellationToken cancellationToken = default)
    {
        var result = await syllabusService.SearchAsync(
            new SyllabusSearchQuery(q, stageCode, versionId, subjectId, kind, pageNumber, pageSize, sortBy),
            TryGetUserId(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("versions/{versionId:guid}/sources")]
    [ProducesResponseType<IReadOnlyList<SyllabusSourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSources(Guid versionId, CancellationToken cancellationToken)
    {
        var tree = await syllabusService.GetTreeAsync(
            versionId,
            TryGetUserId(),
            includeUnpublished: false,
            cancellationToken);
        return tree is null ? NotFound() : Ok(tree.Sources);
    }

    [HttpGet("items/{itemId:guid}/sources")]
    [ProducesResponseType<IReadOnlyList<SyllabusSourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemSources(Guid itemId, CancellationToken cancellationToken) =>
        Ok(await syllabusService.GetItemSourcesAsync(itemId, cancellationToken));

    private Guid? TryGetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? userId : null;
    }
}

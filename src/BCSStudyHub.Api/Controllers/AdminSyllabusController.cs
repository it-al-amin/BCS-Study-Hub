using System.Security.Claims;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Syllabus;
using BCSStudyHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/admin/syllabus")]
[Authorize(Policy = BCSStudyHub.Application.Authorization.PermissionCodes.SyllabusManage)]
public sealed class AdminSyllabusController(ISyllabusService syllabusService) : ControllerBase
{
    [HttpGet("stages")]
    public async Task<IActionResult> GetStages(CancellationToken cancellationToken) =>
        Ok(await syllabusService.GetStagesAsync(cancellationToken));

    [HttpPut("stages/{stageId:guid}")]
    public async Task<IActionResult> UpdateStage(
        Guid stageId,
        [FromBody] UpdateExamStageRequest request,
        CancellationToken cancellationToken)
    {
        await syllabusService.UpdateStageAsync(stageId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions(
        [FromQuery] string? stageCode,
        [FromQuery] string? trackCode,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "updated",
        CancellationToken cancellationToken = default)
    {
        var result = await syllabusService.GetVersionsAsync(
            new SyllabusVersionQuery(stageCode, trackCode, status, pageNumber, pageSize, sortBy),
            includeUnpublished: true,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("stages/{stageId:guid}/versions")]
    [ProducesResponseType<SyllabusVersionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVersion(
        Guid stageId,
        [FromBody] CreateSyllabusVersionRequest request,
        CancellationToken cancellationToken)
    {
        var version = await syllabusService.CreateVersionAsync(stageId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetVersionTree),
            new { versionId = version.Id },
            version);
    }

    [HttpGet("versions/{versionId:guid}/tree")]
    public async Task<IActionResult> GetVersionTree(Guid versionId, CancellationToken cancellationToken)
    {
        var tree = await syllabusService.GetTreeAsync(
            versionId,
            GetUserId(),
            includeUnpublished: true,
            cancellationToken);
        return tree is null ? NotFound() : Ok(tree);
    }

    [HttpPut("versions/{versionId:guid}")]
    public async Task<IActionResult> UpdateVersion(
        Guid versionId,
        [FromBody] UpdateSyllabusVersionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await syllabusService.UpdateVersionAsync(versionId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("versions/{versionId:guid}/publish")]
    public async Task<IActionResult> PublishVersion(Guid versionId, CancellationToken cancellationToken)
    {
        var published = await syllabusService.SetVersionStatusAsync(
            versionId,
            nameof(SyllabusVersionStatus.Published),
            cancellationToken);
        return published is null ? NotFound() : Ok(published);
    }

    [HttpPost("versions/{versionId:guid}/archive")]
    public async Task<IActionResult> ArchiveVersion(Guid versionId, CancellationToken cancellationToken)
    {
        var archived = await syllabusService.SetVersionStatusAsync(
            versionId,
            nameof(SyllabusVersionStatus.Archived),
            cancellationToken);
        return archived is null ? NotFound() : Ok(archived);
    }

    [HttpPost("versions/{versionId:guid}/subjects")]
    public async Task<IActionResult> AssignSubject(
        Guid versionId,
        [FromBody] AssignSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await syllabusService.AssignSubjectAsync(versionId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetVersionTree),
            new { versionId },
            mapping);
    }

    [HttpPut("syllabus-subjects/{syllabusSubjectId:guid}")]
    public async Task<IActionResult> UpdateSyllabusSubject(
        Guid syllabusSubjectId,
        [FromBody] UpdateSyllabusSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await syllabusService.UpdateSyllabusSubjectAsync(syllabusSubjectId, request, cancellationToken);
        return mapping is null ? NotFound() : Ok(mapping);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "name",
        CancellationToken cancellationToken = default)
    {
        var subjects = await syllabusService.GetSubjectsAsync(
            search,
            includeInactive,
            pageNumber,
            pageSize,
            sortBy,
            cancellationToken);
        return Ok(subjects);
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(
        [FromBody] CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var subject = await syllabusService.CreateSubjectAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, subject);
    }

    [HttpPut("subjects/{subjectId:guid}")]
    public async Task<IActionResult> UpdateSubject(
        Guid subjectId,
        [FromBody] UpdateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var subject = await syllabusService.UpdateSubjectAsync(subjectId, request, cancellationToken);
        return subject is null ? NotFound() : Ok(subject);
    }

    [HttpGet("papers")]
    public async Task<IActionResult> GetPapers(
        [FromQuery] Guid syllabusSubjectId,
        CancellationToken cancellationToken)
    {
        var papers = await syllabusService.GetPapersAsync(
            syllabusSubjectId,
            includeUnpublished: true,
            cancellationToken);
        return Ok(papers);
    }

    [HttpPost("syllabus-subjects/{syllabusSubjectId:guid}/papers")]
    public async Task<IActionResult> CreatePaper(
        Guid syllabusSubjectId,
        [FromBody] CreatePaperRequest request,
        CancellationToken cancellationToken)
    {
        var paper = await syllabusService.CreatePaperAsync(syllabusSubjectId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, paper);
    }

    [HttpPut("papers/{paperId:guid}")]
    public async Task<IActionResult> UpdatePaper(
        Guid paperId,
        [FromBody] UpdatePaperRequest request,
        CancellationToken cancellationToken)
    {
        var paper = await syllabusService.UpdatePaperAsync(paperId, request, cancellationToken);
        return paper is null ? NotFound() : Ok(paper);
    }

    [HttpDelete("papers/{paperId:guid}")]
    public async Task<IActionResult> DeletePaper(Guid paperId, CancellationToken cancellationToken) =>
        await syllabusService.DeletePaperAsync(paperId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] Guid versionId,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? parentId,
        [FromQuery] string? kind,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await syllabusService.GetItemsAsync(
            new SyllabusItemQuery(versionId, subjectId, parentId, kind, pageNumber, pageSize),
            includeUnpublished: true,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic(
        [FromBody] CreateSyllabusItemRequest request,
        CancellationToken cancellationToken)
    {
        var topic = await syllabusService.CreateItemAsync(
            SyllabusItemKindRequest.Topic,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, topic);
    }

    [HttpPost("subtopics")]
    public async Task<IActionResult> CreateSubtopic(
        [FromBody] CreateSyllabusItemRequest request,
        CancellationToken cancellationToken)
    {
        var topic = await syllabusService.CreateItemAsync(
            SyllabusItemKindRequest.Subtopic,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, topic);
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid itemId,
        [FromBody] UpdateSyllabusItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await syllabusService.UpdateItemAsync(itemId, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken cancellationToken) =>
        await syllabusService.DeleteItemAsync(itemId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("versions/{versionId:guid}/sources")]
    public async Task<IActionResult> GetSources(Guid versionId, CancellationToken cancellationToken) =>
        Ok(await syllabusService.GetSourcesAsync(versionId, cancellationToken));

    [HttpPost("sources")]
    public async Task<IActionResult> CreateSource(
        [FromBody] CreateSyllabusSourceRequest request,
        CancellationToken cancellationToken)
    {
        var source = await syllabusService.CreateSourceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSource), new { sourceId = source.Id }, source);
    }

    [HttpGet("sources/{sourceId:guid}")]
    public async Task<IActionResult> GetSource(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await syllabusService.GetSourceAsync(sourceId, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    [HttpPut("sources/{sourceId:guid}")]
    public async Task<IActionResult> UpdateSource(
        Guid sourceId,
        [FromBody] UpdateSyllabusSourceRequest request,
        CancellationToken cancellationToken)
    {
        var source = await syllabusService.UpdateSourceAsync(sourceId, request, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    [HttpPut("versions/{versionId:guid}/sources/{sourceId:guid}")]
    public async Task<IActionResult> LinkVersionSource(
        Guid versionId,
        Guid sourceId,
        [FromBody] LinkSyllabusSourceRequest request,
        CancellationToken cancellationToken)
    {
        await syllabusService.LinkVersionSourceAsync(versionId, sourceId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("versions/{versionId:guid}/sources/{sourceId:guid}")]
    public async Task<IActionResult> UnlinkVersionSource(
        Guid versionId,
        Guid sourceId,
        CancellationToken cancellationToken) =>
        await syllabusService.UnlinkVersionSourceAsync(versionId, sourceId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpPut("items/{itemId:guid}/sources/{sourceId:guid}")]
    public async Task<IActionResult> LinkItemSource(
        Guid itemId,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        await syllabusService.LinkItemSourceAsync(itemId, sourceId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("items/{itemId:guid}/sources/{sourceId:guid}")]
    public async Task<IActionResult> UnlinkItemSource(
        Guid itemId,
        Guid sourceId,
        CancellationToken cancellationToken) =>
        await syllabusService.UnlinkItemSourceAsync(itemId, sourceId, cancellationToken)
            ? NoContent()
            : NotFound();

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

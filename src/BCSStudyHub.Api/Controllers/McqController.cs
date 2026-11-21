using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Mcq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/questions")]
[AllowAnonymous]
public sealed class McqController(IMcqService mcqService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<McqQuestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] Guid? syllabusVersionId,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? syllabusItemId,
        [FromQuery] int? bcsExamYear,
        [FromQuery] string? difficulty,
        [FromQuery] string? tag,
        [FromQuery] bool bookmarkedOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "recent",
        CancellationToken cancellationToken = default)
    {
        var result = await mcqService.GetQuestionsAsync(
            new McqQuestionQuery(
                search,
                syllabusVersionId,
                subjectId,
                syllabusItemId,
                bcsExamYear,
                difficulty,
                tag,
                bookmarkedOnly,
                pageNumber,
                pageSize,
                sortBy),
            TryGetUserId(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{questionId:guid}")]
    public async Task<IActionResult> GetQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await mcqService.GetQuestionAsync(questionId, TryGetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

[ApiController]
[Route("api/v1/practice/mcq")]
public sealed class McqPracticeController(IMcqService mcqService) : ControllerBase
{
    [HttpGet("sets")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mcqService.GetExamSetsAsync(
            includeUnpublished: false,
            pageNumber,
            pageSize,
            cancellationToken));

    [HttpPost("attempts")]
    [Authorize]
    public async Task<IActionResult> CreateAttempt(
        [FromBody] CreateMcqAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var attempt = await mcqService.CreateAttemptAsync(
            GetUserId(),
            request.ExamSetId,
            cancellationToken);
        return CreatedAtAction(nameof(GetAttempt), new { attemptId = attempt.Id }, attempt);
    }

    [HttpGet("attempts")]
    [Authorize]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mcqService.GetAttemptHistoryAsync(GetUserId(), pageNumber, pageSize, cancellationToken));

    [HttpGet("attempts/{attemptId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await mcqService.GetAttemptAsync(GetUserId(), attemptId, cancellationToken);
        return attempt is null ? NotFound() : Ok(attempt);
    }

    [HttpPut("attempts/{attemptId:guid}/questions/{attemptQuestionId:guid}/answer")]
    [Authorize]
    public async Task<IActionResult> UpdateAnswer(
        Guid attemptId,
        Guid attemptQuestionId,
        [FromBody] UpdateMcqAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mcqService.UpdateAnswerAsync(
            GetUserId(),
            attemptId,
            attemptQuestionId,
            request,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("attempts/{attemptId:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await mcqService.SubmitAttemptAsync(GetUserId(), attemptId, cancellationToken);
        return attempt is null ? NotFound() : Ok(attempt);
    }

    [HttpGet("attempts/{attemptId:guid}/review")]
    [Authorize]
    public async Task<IActionResult> GetReview(Guid attemptId, CancellationToken cancellationToken)
    {
        var review = await mcqService.GetAttemptReviewAsync(GetUserId(), attemptId, cancellationToken);
        return review is null ? NotFound() : Ok(review);
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new McqException(StatusCodes.Status401Unauthorized, "Authentication required", "A valid user identity is required.");
    }
}

[ApiController]
[Route("api/v1/me/questions")]
[Authorize]
public sealed class McqPersonalController(IMcqService mcqService) : ControllerBase
{
    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mcqService.GetBookmarksAsync(GetUserId(), pageNumber, pageSize, cancellationToken));

    [HttpPost("{questionId:guid}/bookmark")]
    public async Task<IActionResult> AddBookmark(Guid questionId, CancellationToken cancellationToken)
    {
        await mcqService.AddBookmarkAsync(GetUserId(), questionId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{questionId:guid}/bookmark")]
    public async Task<IActionResult> RemoveBookmark(Guid questionId, CancellationToken cancellationToken) =>
        await mcqService.RemoveBookmarkAsync(GetUserId(), questionId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpGet("mistakes")]
    public async Task<IActionResult> GetMistakes(
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mcqService.GetMistakesAsync(GetUserId(), status, pageNumber, pageSize, cancellationToken));

    [HttpPut("{questionId:guid}/mistake")]
    public async Task<IActionResult> UpdateMistake(
        Guid questionId,
        [FromBody] UpdateMcqMistakeRequest request,
        CancellationToken cancellationToken)
    {
        var mistake = await mcqService.UpdateMistakeAsync(GetUserId(), questionId, request, cancellationToken);
        return mistake is null ? NotFound() : Ok(mistake);
    }

    [HttpGet("/api/v1/me/mcq/analytics")]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken) =>
        Ok(await mcqService.GetAnalyticsAsync(GetUserId(), cancellationToken));

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new McqException(StatusCodes.Status401Unauthorized, "Authentication required", "A valid user identity is required.");
    }
}

[ApiController]
[Route("api/v1/admin/mcq")]
[Authorize(Policy = BCSStudyHub.Application.Authorization.PermissionCodes.McqManage)]
public sealed class AdminMcqController(IMcqService mcqService) : ControllerBase
{
    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mcqService.GetAdminQuestionsAsync(search, status, pageNumber, pageSize, cancellationToken));

    [HttpGet("questions/{questionId:guid}")]
    public async Task<IActionResult> GetQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await mcqService.GetAdminQuestionAsync(questionId, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] McqQuestionInput input,
        CancellationToken cancellationToken)
    {
        var question = await mcqService.CreateQuestionAsync(input, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetQuestion), new { questionId = question.Id }, question);
    }

    [HttpPut("questions/{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid questionId,
        [FromBody] McqQuestionInput input,
        CancellationToken cancellationToken)
    {
        var question = await mcqService.UpdateQuestionAsync(questionId, input, GetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost("questions/{questionId:guid}/publish")]
    public async Task<IActionResult> PublishQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await mcqService.SetQuestionStatusAsync(
            questionId,
            "Published",
            GetUserId(),
            cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost("questions/{questionId:guid}/archive")]
    public async Task<IActionResult> ArchiveQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await mcqService.SetQuestionStatusAsync(
            questionId,
            "Archived",
            GetUserId(),
            cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpGet("sets")]
    public async Task<IActionResult> GetSets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await mcqService.GetExamSetsAsync(
            includeUnpublished: true,
            pageNumber,
            pageSize,
            cancellationToken));

    [HttpGet("sets/{setId:guid}")]
    public async Task<IActionResult> GetSet(Guid setId, CancellationToken cancellationToken)
    {
        var set = await mcqService.GetAdminExamSetAsync(setId, cancellationToken);
        return set is null ? NotFound() : Ok(set);
    }

    [HttpPost("sets")]
    public async Task<IActionResult> CreateSet(
        [FromBody] McqExamSetInput input,
        CancellationToken cancellationToken)
    {
        var set = await mcqService.CreateExamSetAsync(input, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetSet), new { setId = set.Id }, set);
    }

    [HttpPut("sets/{setId:guid}")]
    public async Task<IActionResult> UpdateSet(
        Guid setId,
        [FromBody] McqExamSetInput input,
        CancellationToken cancellationToken)
    {
        var set = await mcqService.UpdateExamSetAsync(setId, input, GetUserId(), cancellationToken);
        return set is null ? NotFound() : Ok(set);
    }

    [HttpPost("sets/{setId:guid}/publish")]
    public async Task<IActionResult> PublishSet(Guid setId, CancellationToken cancellationToken)
    {
        var set = await mcqService.SetExamSetStatusAsync(setId, "Published", GetUserId(), cancellationToken);
        return set is null ? NotFound() : Ok(set);
    }

    [HttpPost("sets/{setId:guid}/archive")]
    public async Task<IActionResult> ArchiveSet(Guid setId, CancellationToken cancellationToken)
    {
        var set = await mcqService.SetExamSetStatusAsync(setId, "Archived", GetUserId(), cancellationToken);
        return set is null ? NotFound() : Ok(set);
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new McqException(StatusCodes.Status401Unauthorized, "Authentication required", "A valid user identity is required.");
    }
}

public sealed record CreateMcqAttemptRequest([Required] Guid ExamSetId);

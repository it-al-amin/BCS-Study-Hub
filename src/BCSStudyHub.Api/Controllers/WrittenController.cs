using System.Security.Claims;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Written;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/written/questions")]
[AllowAnonymous]
public sealed class WrittenController(IWrittenService writtenService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] Guid? syllabusVersionId,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? syllabusItemId,
        [FromQuery] string? type,
        [FromQuery] int? bcsExamYear,
        [FromQuery] string? tag,
        [FromQuery] bool bookmarkedOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "recent",
        CancellationToken cancellationToken = default)
    {
        var result = await writtenService.GetQuestionsAsync(
            new WrittenQuestionQuery(
                search,
                syllabusVersionId,
                subjectId,
                syllabusItemId,
                type,
                bcsExamYear,
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
        var question = await writtenService.GetQuestionAsync(questionId, TryGetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpGet("{questionId:guid}/model-answer")]
    public async Task<IActionResult> GetModelAnswer(Guid questionId, CancellationToken cancellationToken)
    {
        var answer = await writtenService.GetModelAnswerAsync(questionId, cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

[ApiController]
[Route("api/v1/me/written")]
[Authorize]
public sealed class WrittenPersonalController(IWrittenService writtenService) : ControllerBase
{
    [HttpGet("answers")]
    public async Task<IActionResult> GetAnswers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await writtenService.GetAnswersAsync(GetUserId(), pageNumber, pageSize, cancellationToken));

    [HttpPost("questions/{questionId:guid}/answers")]
    public async Task<IActionResult> CreateAnswer(Guid questionId, CancellationToken cancellationToken)
    {
        var answer = await writtenService.CreateAnswerAsync(GetUserId(), questionId, cancellationToken);
        return CreatedAtAction(nameof(GetAnswer), new { answerId = answer.Id }, answer);
    }

    [HttpGet("answers/{answerId:guid}")]
    public async Task<IActionResult> GetAnswer(Guid answerId, CancellationToken cancellationToken)
    {
        var answer = await writtenService.GetAnswerAsync(GetUserId(), answerId, cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    [HttpPut("answers/{answerId:guid}")]
    public async Task<IActionResult> UpdateAnswer(
        Guid answerId,
        [FromBody] UpdateWrittenAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var answer = await writtenService.UpdateAnswerAsync(GetUserId(), answerId, request, cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    [HttpPost("answers/{answerId:guid}/submit")]
    public async Task<IActionResult> SubmitAnswer(
        Guid answerId,
        [FromBody] UpdateWrittenAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var answer = await writtenService.SubmitAnswerAsync(GetUserId(), answerId, request, cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    [HttpPost("answers/{answerId:guid}/revise")]
    public async Task<IActionResult> ReviseAnswer(Guid answerId, CancellationToken cancellationToken)
    {
        var answer = await writtenService.ReviseAnswerAsync(GetUserId(), answerId, cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    [HttpGet("answers/{answerId:guid}/revisions")]
    public async Task<IActionResult> GetRevisions(Guid answerId, CancellationToken cancellationToken)
    {
        var revisions = await writtenService.GetAnswerRevisionsAsync(GetUserId(), answerId, cancellationToken);
        return revisions is null ? NotFound() : Ok(revisions);
    }

    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await writtenService.GetBookmarksAsync(GetUserId(), pageNumber, pageSize, cancellationToken));

    [HttpPost("questions/{questionId:guid}/bookmark")]
    public async Task<IActionResult> AddBookmark(Guid questionId, CancellationToken cancellationToken)
    {
        await writtenService.AddBookmarkAsync(GetUserId(), questionId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("questions/{questionId:guid}/bookmark")]
    public async Task<IActionResult> RemoveBookmark(Guid questionId, CancellationToken cancellationToken) =>
        await writtenService.RemoveBookmarkAsync(GetUserId(), questionId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals(CancellationToken cancellationToken) =>
        Ok(await writtenService.GetGoalsAsync(GetUserId(), cancellationToken));

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal(
        [FromBody] WrittenGoalInput input,
        CancellationToken cancellationToken) =>
        Ok(await writtenService.CreateGoalAsync(GetUserId(), input, cancellationToken));

    [HttpPut("goals/{goalId:guid}")]
    public async Task<IActionResult> UpdateGoal(
        Guid goalId,
        [FromBody] WrittenGoalInput input,
        CancellationToken cancellationToken)
    {
        var goal = await writtenService.UpdateGoalAsync(GetUserId(), goalId, input, cancellationToken);
        return goal is null ? NotFound() : Ok(goal);
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await writtenService.GetProgressAsync(GetUserId(), from, to, cancellationToken));

    [HttpGet("notes")]
    public async Task<IActionResult> GetStudyNotes(
        [FromQuery] Guid? syllabusItemId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await writtenService.GetStudyNotesAsync(GetUserId(), syllabusItemId, pageNumber, pageSize, cancellationToken));

    [HttpPost("notes")]
    public async Task<IActionResult> CreateStudyNote(
        [FromBody] WrittenStudyNoteInput input,
        CancellationToken cancellationToken) =>
        Ok(await writtenService.CreateStudyNoteAsync(GetUserId(), input, cancellationToken));

    [HttpPut("notes/{noteId:guid}")]
    public async Task<IActionResult> UpdateStudyNote(
        Guid noteId,
        [FromBody] WrittenStudyNoteInput input,
        CancellationToken cancellationToken)
    {
        var note = await writtenService.UpdateStudyNoteAsync(GetUserId(), noteId, input, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpDelete("notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteStudyNote(Guid noteId, CancellationToken cancellationToken) =>
        await writtenService.DeleteStudyNoteAsync(GetUserId(), noteId, cancellationToken)
            ? NoContent()
            : NotFound();

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new WrittenException(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid user identity is required.");
    }
}

[ApiController]
[Route("api/v1/admin/written/questions")]
[Authorize(Policy = BCSStudyHub.Application.Authorization.PermissionCodes.WrittenManage)]
public sealed class AdminWrittenController(IWrittenService writtenService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await writtenService.GetAdminQuestionsAsync(search, status, pageNumber, pageSize, cancellationToken));

    [HttpGet("{questionId:guid}")]
    public async Task<IActionResult> GetQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await writtenService.GetAdminQuestionAsync(questionId, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] WrittenQuestionInput input,
        CancellationToken cancellationToken)
    {
        var question = await writtenService.CreateQuestionAsync(input, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetQuestion), new { questionId = question.Question.Id }, question);
    }

    [HttpPut("{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid questionId,
        [FromBody] WrittenQuestionInput input,
        CancellationToken cancellationToken)
    {
        var question = await writtenService.UpdateQuestionAsync(questionId, input, GetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost("{questionId:guid}/publish")]
    public Task<IActionResult> PublishQuestion(Guid questionId, CancellationToken cancellationToken) =>
        UpdateQuestionStatus(questionId, "Published", cancellationToken);

    [HttpPost("{questionId:guid}/archive")]
    public Task<IActionResult> ArchiveQuestion(Guid questionId, CancellationToken cancellationToken) =>
        UpdateQuestionStatus(questionId, "Archived", cancellationToken);

    [HttpPost("{questionId:guid}/model-answer/publish")]
    public async Task<IActionResult> PublishModelAnswer(Guid questionId, CancellationToken cancellationToken)
    {
        var answer = await writtenService.SetModelAnswerStatusAsync(questionId, "Published", GetUserId(), cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    [HttpPost("{questionId:guid}/model-answer/archive")]
    public async Task<IActionResult> ArchiveModelAnswer(Guid questionId, CancellationToken cancellationToken)
    {
        var answer = await writtenService.SetModelAnswerStatusAsync(questionId, "Archived", GetUserId(), cancellationToken);
        return answer is null ? NotFound() : Ok(answer);
    }

    private async Task<IActionResult> UpdateQuestionStatus(
        Guid questionId,
        string status,
        CancellationToken cancellationToken)
    {
        var question = await writtenService.SetQuestionStatusAsync(questionId, status, GetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new WrittenException(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid user identity is required.");
    }
}

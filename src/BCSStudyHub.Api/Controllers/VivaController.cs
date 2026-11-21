using System.Security.Claims;
using BCSStudyHub.Application.Viva;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/viva")]
[AllowAnonymous]
public sealed class VivaController(IVivaService vivaService) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(await vivaService.GetCategoriesAsync(includeInactive: false, cancellationToken));

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics([FromQuery] Guid? categoryId, CancellationToken cancellationToken) =>
        Ok(await vivaService.GetTopicsAsync(categoryId, cancellationToken));

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? topic,
        [FromQuery] bool bookmarkedOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "recent",
        CancellationToken cancellationToken = default) =>
        Ok(await vivaService.GetQuestionsAsync(
            new VivaQuestionQuery(search, categoryId, topic, bookmarkedOnly, pageNumber, pageSize, sortBy),
            TryGetUserId(),
            cancellationToken));

    [HttpGet("questions/{questionId:guid}")]
    public async Task<IActionResult> GetQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await vivaService.GetQuestionAsync(questionId, TryGetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

[ApiController]
[Route("api/v1/admin/viva/categories")]
[Authorize(Policy = BCSStudyHub.Application.Authorization.PermissionCodes.VivaManage)]
public sealed class AdminVivaCategoriesController(IVivaService vivaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(await vivaService.GetAdminCategoriesAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        [FromBody] VivaCategoryInput input,
        CancellationToken cancellationToken)
    {
        var category = await vivaService.CreateCategoryAsync(input, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetCategories), category);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid categoryId,
        [FromBody] VivaCategoryInput input,
        CancellationToken cancellationToken)
    {
        var category = await vivaService.UpdateCategoryAsync(categoryId, input, GetUserId(), cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    private Guid GetUserId() => VivaControllerUser.GetUserId(User);
}

[ApiController]
[Route("api/v1/admin/viva/questions")]
[Authorize(Policy = BCSStudyHub.Application.Authorization.PermissionCodes.VivaManage)]
public sealed class AdminVivaQuestionsController(IVivaService vivaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await vivaService.GetAdminQuestionsAsync(search, status, pageNumber, pageSize, cancellationToken));

    [HttpGet("{questionId:guid}")]
    public async Task<IActionResult> GetQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await vivaService.GetAdminQuestionAsync(questionId, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] VivaQuestionInput input,
        CancellationToken cancellationToken)
    {
        var question = await vivaService.CreateQuestionAsync(input, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetQuestion), new { questionId = question.Question.Id }, question);
    }

    [HttpPut("{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid questionId,
        [FromBody] VivaQuestionInput input,
        CancellationToken cancellationToken)
    {
        var question = await vivaService.UpdateQuestionAsync(questionId, input, GetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost("{questionId:guid}/publish")]
    public Task<IActionResult> PublishQuestion(Guid questionId, CancellationToken cancellationToken) =>
        UpdateQuestionStatus(questionId, "Published", cancellationToken);

    [HttpPost("{questionId:guid}/archive")]
    public Task<IActionResult> ArchiveQuestion(Guid questionId, CancellationToken cancellationToken) =>
        UpdateQuestionStatus(questionId, "Archived", cancellationToken);

    private async Task<IActionResult> UpdateQuestionStatus(
        Guid questionId,
        string status,
        CancellationToken cancellationToken)
    {
        var question = await vivaService.SetQuestionStatusAsync(questionId, status, GetUserId(), cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    private Guid GetUserId() => VivaControllerUser.GetUserId(User);
}

[ApiController]
[Route("api/v1/me/viva")]
[Authorize]
public sealed class VivaPersonalController(IVivaService vivaService) : ControllerBase
{
    [HttpPost("questions/{questionId:guid}/bookmark")]
    public async Task<IActionResult> AddBookmark(Guid questionId, CancellationToken cancellationToken) =>
        await vivaService.AddBookmarkAsync(GetUserId(), questionId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpDelete("questions/{questionId:guid}/bookmark")]
    public async Task<IActionResult> RemoveBookmark(Guid questionId, CancellationToken cancellationToken) =>
        await vivaService.RemoveBookmarkAsync(GetUserId(), questionId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpGet("questions/{questionId:guid}/answer-note")]
    public async Task<IActionResult> GetAnswerNote(Guid questionId, CancellationToken cancellationToken)
    {
        var note = await vivaService.GetAnswerNoteAsync(GetUserId(), questionId, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpPut("questions/{questionId:guid}/answer-note")]
    public async Task<IActionResult> SaveAnswerNote(
        Guid questionId,
        [FromBody] VivaAnswerNoteInput input,
        CancellationToken cancellationToken) =>
        Ok(await vivaService.SaveAnswerNoteAsync(GetUserId(), questionId, input, cancellationToken));

    [HttpGet("question-history")]
    public async Task<IActionResult> GetQuestionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await vivaService.GetQuestionHistoryAsync(GetUserId(), pageNumber, pageSize, cancellationToken));

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await vivaService.GetSessionsAsync(GetUserId(), pageNumber, pageSize, cancellationToken));

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession(
        [FromBody] VivaMockSessionInput input,
        CancellationToken cancellationToken)
    {
        var session = await vivaService.CreateSessionAsync(GetUserId(), input, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session);
    }

    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await vivaService.GetSessionAsync(GetUserId(), sessionId, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPut("sessions/{sessionId:guid}/questions/{sessionQuestionId:guid}")]
    public async Task<IActionResult> UpdateSessionQuestion(
        Guid sessionId,
        Guid sessionQuestionId,
        [FromBody] VivaMockSessionQuestionInput input,
        CancellationToken cancellationToken)
    {
        var session = await vivaService.UpdateSessionQuestionAsync(
            GetUserId(),
            sessionId,
            sessionQuestionId,
            input,
            cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    public async Task<IActionResult> CompleteSession(
        Guid sessionId,
        [FromBody] VivaSessionEvaluationInput input,
        CancellationToken cancellationToken)
    {
        var session = await vivaService.CompleteSessionAsync(GetUserId(), sessionId, input, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("weak-topics")]
    public async Task<IActionResult> GetWeakTopics(CancellationToken cancellationToken) =>
        Ok(await vivaService.GetWeakTopicsAsync(GetUserId(), cancellationToken));

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken) =>
        Ok(await vivaService.GetProfileAsync(GetUserId(), cancellationToken));

    [HttpPut("profile")]
    public async Task<IActionResult> SaveProfile(
        [FromBody] VivaProfileInput input,
        CancellationToken cancellationToken) =>
        Ok(await vivaService.SaveProfileAsync(GetUserId(), input, cancellationToken));

    private Guid GetUserId() => VivaControllerUser.GetUserId(User);
}

internal static class VivaControllerUser
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new VivaException(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid user identity is required.");
    }
}

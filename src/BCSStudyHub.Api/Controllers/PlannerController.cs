using System.Security.Claims;
using BCSStudyHub.Application.Planner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

internal static class PlannerHttp
{
    public static Guid UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new PlannerException(StatusCodes.Status401Unauthorized, "Authentication required", "A valid user identity is required.");
}

/// <summary>Personal planner: settings, goals, tasks, schedules, sessions, and the calendar. All data is owner-scoped.</summary>
[ApiController]
[Route("api/v1/me/planner")]
[Authorize]
public sealed class PlannerController(IPlannerService planner) : ControllerBase
{
    private Guid UserId => PlannerHttp.UserId(User);

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken) => Ok(await planner.GetSettingsAsync(UserId, cancellationToken));

    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] PlannerSettingsInput input, CancellationToken cancellationToken) =>
        Ok(await planner.SaveSettingsAsync(UserId, input, cancellationToken));

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals([FromQuery] string? period, [FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        Ok(await planner.GetGoalsAsync(UserId, period, date, cancellationToken));

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal([FromBody] StudyGoalInput input, CancellationToken cancellationToken) =>
        Ok(await planner.CreateGoalAsync(UserId, input, cancellationToken));

    [HttpPut("goals/{goalId:guid}")]
    public async Task<IActionResult> UpdateGoal(Guid goalId, [FromBody] StudyGoalInput input, CancellationToken cancellationToken) =>
        await planner.UpdateGoalAsync(UserId, goalId, input, cancellationToken) is { } goal ? Ok(goal) : NotFound();

    [HttpDelete("goals/{goalId:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid goalId, CancellationToken cancellationToken) =>
        await planner.DeleteGoalAsync(UserId, goalId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? status,
        [FromQuery] Guid? subjectId,
        [FromQuery] string? stage,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await planner.GetTasksAsync(UserId, new StudyTaskQuery(from, to, status, subjectId, stage, pageNumber, pageSize), cancellationToken));

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] StudyTaskInput input, CancellationToken cancellationToken) =>
        Ok(await planner.CreateTaskAsync(UserId, input, cancellationToken));

    [HttpPut("tasks/{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(Guid taskId, [FromBody] StudyTaskInput input, CancellationToken cancellationToken) =>
        await planner.UpdateTaskAsync(UserId, taskId, input, cancellationToken) is { } task ? Ok(task) : NotFound();

    [HttpPost("tasks/{taskId:guid}/status")]
    public async Task<IActionResult> SetTaskStatus(Guid taskId, [FromBody] StudyTaskStatusInput input, CancellationToken cancellationToken) =>
        await planner.SetTaskStatusAsync(UserId, taskId, input, cancellationToken) is { } task ? Ok(task) : NotFound();

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid taskId, CancellationToken cancellationToken) =>
        await planner.DeleteTaskAsync(UserId, taskId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules(CancellationToken cancellationToken) => Ok(await planner.GetSchedulesAsync(UserId, cancellationToken));

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] StudyScheduleInput input, CancellationToken cancellationToken) =>
        Ok(await planner.CreateScheduleAsync(UserId, input, cancellationToken));

    [HttpPut("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid scheduleId, [FromBody] StudyScheduleInput input, CancellationToken cancellationToken) =>
        await planner.UpdateScheduleAsync(UserId, scheduleId, input, cancellationToken) is { } schedule ? Ok(schedule) : NotFound();

    [HttpDelete("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid scheduleId, CancellationToken cancellationToken) =>
        await planner.DeleteScheduleAsync(UserId, scheduleId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("schedules/{scheduleId:guid}/generate")]
    public async Task<IActionResult> GenerateTasks(Guid scheduleId, [FromBody] GenerateTasksInput input, CancellationToken cancellationToken) =>
        await planner.GenerateTasksAsync(UserId, scheduleId, input, cancellationToken) is int created ? Ok(new { created }) : NotFound();

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await planner.GetSessionsAsync(UserId, from, to, pageNumber, pageSize, cancellationToken));

    [HttpGet("sessions/active")]
    public async Task<IActionResult> GetActiveSession(CancellationToken cancellationToken) =>
        await planner.GetActiveSessionAsync(UserId, cancellationToken) is { } session ? Ok(session) : NoContent();

    [HttpPost("sessions/start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionInput input, CancellationToken cancellationToken) =>
        Ok(await planner.StartSessionAsync(UserId, input, cancellationToken));

    [HttpPost("sessions/{sessionId:guid}/stop")]
    public async Task<IActionResult> StopSession(Guid sessionId, [FromBody] StopSessionRequest? request, CancellationToken cancellationToken) =>
        await planner.StopSessionAsync(UserId, sessionId, request?.Notes, cancellationToken) is { } session ? Ok(session) : NotFound();

    [HttpPost("sessions")]
    public async Task<IActionResult> LogSession([FromBody] StudySessionInput input, CancellationToken cancellationToken) =>
        Ok(await planner.LogSessionAsync(UserId, input, cancellationToken));

    [HttpPut("sessions/{sessionId:guid}")]
    public async Task<IActionResult> UpdateSession(Guid sessionId, [FromBody] StudySessionInput input, CancellationToken cancellationToken) =>
        await planner.UpdateSessionAsync(UserId, sessionId, input, cancellationToken) is { } session ? Ok(session) : NotFound();

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken) =>
        await planner.DeleteSessionAsync(UserId, sessionId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) =>
        Ok(await planner.GetCalendarAsync(UserId, from, to, cancellationToken));
}

public sealed record StopSessionRequest(string? Notes);

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class RevisionController(IRevisionService revision) : ControllerBase
{
    private Guid UserId => PlannerHttp.UserId(User);

    [HttpGet("revision/queue")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] bool dueOnly = true,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await revision.GetQueueAsync(UserId, dueOnly, sourceType, status, pageNumber, pageSize, cancellationToken));

    [HttpPost("revision/items")]
    public async Task<IActionResult> AddItem([FromBody] RevisionItemInput input, CancellationToken cancellationToken) =>
        Ok(await revision.AddItemAsync(UserId, input, cancellationToken));

    [HttpPost("revision/items/{itemId:guid}/review")]
    public async Task<IActionResult> Review(Guid itemId, [FromBody] RevisionReviewInput input, CancellationToken cancellationToken) =>
        await revision.ReviewAsync(UserId, itemId, input, cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpPost("revision/items/{itemId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid itemId, CancellationToken cancellationToken) =>
        await revision.SetItemStatusAsync(UserId, itemId, "Suspended", cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpPost("revision/items/{itemId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid itemId, CancellationToken cancellationToken) =>
        await revision.SetItemStatusAsync(UserId, itemId, "Active", cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpDelete("revision/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken cancellationToken) =>
        await revision.DeleteItemAsync(UserId, itemId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("revision/import")]
    public async Task<IActionResult> Import([FromBody] RevisionImportInput input, CancellationToken cancellationToken) =>
        Ok(await revision.ImportAsync(UserId, input, cancellationToken));

    [HttpGet("revision/history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await revision.GetHistoryAsync(UserId, from, to, pageNumber, pageSize, cancellationToken));

    [HttpGet("revision/weak-topics")]
    public async Task<IActionResult> GetWeakTopics(CancellationToken cancellationToken) => Ok(await revision.GetWeakTopicsAsync(UserId, cancellationToken));

    [HttpGet("mistakes")]
    public async Task<IActionResult> GetMistakes(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? sourceType,
        [FromQuery] Guid? syllabusItemId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await revision.GetMistakesAsync(UserId, new MistakeQuery(search, status, category, sourceType, syllabusItemId, pageNumber, pageSize), cancellationToken));

    [HttpGet("mistakes/{mistakeId:guid}")]
    public async Task<IActionResult> GetMistake(Guid mistakeId, CancellationToken cancellationToken) =>
        await revision.GetMistakeAsync(UserId, mistakeId, cancellationToken) is { } mistake ? Ok(mistake) : NotFound();

    [HttpPost("mistakes")]
    public async Task<IActionResult> CreateMistake([FromBody] MistakeInput input, CancellationToken cancellationToken) =>
        Ok(await revision.CreateMistakeAsync(UserId, input, cancellationToken));

    [HttpPut("mistakes/{mistakeId:guid}")]
    public async Task<IActionResult> UpdateMistake(Guid mistakeId, [FromBody] MistakeInput input, CancellationToken cancellationToken) =>
        await revision.UpdateMistakeAsync(UserId, mistakeId, input, cancellationToken) is { } mistake ? Ok(mistake) : NotFound();

    [HttpPost("mistakes/{mistakeId:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid mistakeId, CancellationToken cancellationToken) =>
        await revision.SetMistakeStatusAsync(UserId, mistakeId, "Resolved", cancellationToken) is { } mistake ? Ok(mistake) : NotFound();

    [HttpPost("mistakes/{mistakeId:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid mistakeId, CancellationToken cancellationToken) =>
        await revision.SetMistakeStatusAsync(UserId, mistakeId, "Open", cancellationToken) is { } mistake ? Ok(mistake) : NotFound();

    [HttpDelete("mistakes/{mistakeId:guid}")]
    public async Task<IActionResult> DeleteMistake(Guid mistakeId, CancellationToken cancellationToken) =>
        await revision.DeleteMistakeAsync(UserId, mistakeId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("mistakes/import-mcq")]
    public async Task<IActionResult> ImportMcq(CancellationToken cancellationToken) =>
        Ok(new { created = await revision.ImportMcqMistakesAsync(UserId, cancellationToken) });
}

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class ProgressController(IProgressService progress) : ControllerBase
{
    private Guid UserId => PlannerHttp.UserId(User);

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken) => Ok(await progress.GetDashboardAsync(UserId, cancellationToken));

    [HttpGet("progress")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken) => Ok(await progress.GetOverviewAsync(UserId, cancellationToken));

    [HttpGet("progress/topics")]
    public async Task<IActionResult> GetTopics([FromQuery] Guid versionId, [FromQuery] Guid? subjectId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await progress.GetTopicsAsync(UserId, versionId, subjectId, status, cancellationToken));

    [HttpPut("progress/topics/{syllabusItemId:guid}/mastery")]
    public async Task<IActionResult> SetMastery(Guid syllabusItemId, [FromBody] MasteryInput input, CancellationToken cancellationToken) =>
        await progress.SetMasteryAsync(UserId, syllabusItemId, input, cancellationToken) is { } topic ? Ok(topic) : NotFound();

    [HttpGet("reports")]
    public async Task<IActionResult> GetReport([FromQuery] string period = "weekly", [FromQuery] DateOnly? date = null, CancellationToken cancellationToken = default) =>
        Ok(await progress.GetReportAsync(UserId, period, date, cancellationToken));
}

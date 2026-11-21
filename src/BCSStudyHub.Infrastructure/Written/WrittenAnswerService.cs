using System.Text.Json;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Written;
using BCSStudyHub.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Written;

internal sealed partial class WrittenService
{
    public async Task<WrittenAnswerDto> CreateAnswerAsync(
        Guid userId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var question = await PublishedWrittenQuestions()
            .SingleOrDefaultAsync(candidate => candidate.Id == questionId, cancellationToken);
        if (question is null)
        {
            throw NotFound("The published Written question was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var answer = new WrittenAnswer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = questionId,
            Question = question,
            Status = WrittenAnswerStatus.Draft,
            StartedAt = now,
            UpdatedAt = now,
        };
        dbContext.WrittenAnswers.Add(answer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAnswerDtoAsync(userId, answer.Id, cancellationToken);
    }

    public async Task<WrittenAnswerDto?> GetAnswerAsync(
        Guid userId,
        Guid answerId,
        CancellationToken cancellationToken)
    {
        var answer = await LoadAnswerAsync(userId, answerId, cancellationToken);
        return answer is null ? null : ToAnswerDto(answer);
    }

    public async Task<PagedResponse<WrittenAnswerListItemDto>> GetAnswersAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var answers = dbContext.WrittenAnswers.AsNoTracking()
            .Where(answer => answer.UserId == userId)
            .OrderByDescending(answer => answer.UpdatedAt)
            .ThenBy(answer => answer.Id);
        var total = await answers.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await answers
            .Include(answer => answer.Question)
                .ThenInclude(question => question.SyllabusItem)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<WrittenAnswerListItemDto>(
            rows.Select(ToAnswerListItem).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<WrittenAnswerDto?> UpdateAnswerAsync(
        Guid userId,
        Guid answerId,
        UpdateWrittenAnswerRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAnswerInput(request);
        var answer = await LoadAnswerAsync(userId, answerId, cancellationToken, includeRevisions: true);
        if (answer is null)
        {
            return null;
        }

        EnsureDraft(answer);
        ApplyAnswerInput(answer, request);
        AddAnswerRevision(answer, WrittenAnswerRevisionKind.DraftSave);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAnswerDto(answer);
    }

    public async Task<WrittenAnswerDto?> SubmitAnswerAsync(
        Guid userId,
        Guid answerId,
        UpdateWrittenAnswerRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAnswerInput(request);
        var answer = await LoadAnswerAsync(userId, answerId, cancellationToken, includeRevisions: true);
        if (answer is null)
        {
            return null;
        }

        EnsureDraft(answer);
        ApplyAnswerInput(answer, request);
        answer.Status = WrittenAnswerStatus.Submitted;
        answer.SubmittedAt = DateTimeOffset.UtcNow;
        AddAnswerRevision(answer, WrittenAnswerRevisionKind.Submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAnswerDto(answer);
    }

    public async Task<WrittenAnswerDto?> ReviseAnswerAsync(
        Guid userId,
        Guid answerId,
        CancellationToken cancellationToken)
    {
        var answer = await LoadAnswerAsync(userId, answerId, cancellationToken, includeRevisions: true);
        if (answer is null)
        {
            return null;
        }

        if (answer.Status != WrittenAnswerStatus.Submitted)
        {
            throw new WrittenException(
                StatusCodes.Status409Conflict,
                "Answer is not submitted",
                "Only a submitted answer can be revised.");
        }

        answer.Status = WrittenAnswerStatus.Draft;
        answer.UpdatedAt = DateTimeOffset.UtcNow;
        AddAnswerRevision(answer, WrittenAnswerRevisionKind.RevisionStarted);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAnswerDto(answer);
    }

    public async Task<IReadOnlyList<WrittenAnswerRevisionDto>?> GetAnswerRevisionsAsync(
        Guid userId,
        Guid answerId,
        CancellationToken cancellationToken)
    {
        var answer = await dbContext.WrittenAnswers.AsNoTracking()
            .Include(candidate => candidate.Revisions)
            .SingleOrDefaultAsync(candidate => candidate.Id == answerId && candidate.UserId == userId, cancellationToken);
        return answer?.Revisions.OrderBy(revision => revision.RevisionNumber)
            .Select(ToRevisionDto)
            .ToArray();
    }

    public async Task<PagedResponse<WrittenBookmarkDto>> GetBookmarksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var bookmarks = dbContext.WrittenBookmarks.AsNoTracking()
            .Where(bookmark =>
                bookmark.UserId == userId &&
                bookmark.Question.Status == WrittenQuestionStatus.Published &&
                bookmark.Question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                bookmark.Question.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Written")
            .OrderByDescending(bookmark => bookmark.CreatedAt);
        var total = await bookmarks.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await bookmarks
            .Include(bookmark => bookmark.Question)
                .ThenInclude(question => question.SyllabusItem)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<WrittenBookmarkDto>(
            rows.Select(bookmark => new WrittenBookmarkDto(
                bookmark.QuestionId,
                bookmark.Question.Prompt,
                bookmark.Question.Type.ToString(),
                bookmark.Question.SyllabusItemId,
                bookmark.Question.SyllabusItem.Title,
                bookmark.CreatedAt)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<bool> AddBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken)
    {
        if (!await PublishedWrittenQuestions().AnyAsync(question => question.Id == questionId, cancellationToken))
        {
            throw NotFound("The published Written question was not found.");
        }

        if (await dbContext.WrittenBookmarks.AnyAsync(
                bookmark => bookmark.UserId == userId && bookmark.QuestionId == questionId,
                cancellationToken))
        {
            return false;
        }

        dbContext.WrittenBookmarks.Add(new WrittenBookmark
        {
            UserId = userId,
            QuestionId = questionId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken)
    {
        var bookmark = await dbContext.WrittenBookmarks.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.QuestionId == questionId,
            cancellationToken);
        if (bookmark is null)
        {
            return false;
        }

        dbContext.WrittenBookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<WrittenGoalDto>> GetGoalsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var goals = await dbContext.WrittenGoals.AsNoTracking()
            .Where(goal => goal.UserId == userId)
            .OrderByDescending(goal => goal.StartDate)
            .ToListAsync(cancellationToken);
        if (goals.Count == 0)
        {
            return [];
        }

        var submitted = await dbContext.WrittenAnswerRevisions.AsNoTracking()
            .Where(revision =>
                revision.Answer.UserId == userId &&
                revision.Kind == WrittenAnswerRevisionKind.Submission)
            .Select(revision => new { revision.CreatedAt, revision.WordCount })
            .ToListAsync(cancellationToken);
        return goals.Select(goal =>
        {
            var completed = submitted.Where(answer =>
            {
                var date = DateOnly.FromDateTime(answer.CreatedAt.UtcDateTime);
                return date >= goal.StartDate && date <= goal.EndDate;
            }).ToArray();
            return new WrittenGoalDto(
                goal.Id,
                goal.StartDate,
                goal.EndDate,
                goal.TargetSubmissions,
                goal.TargetWords,
                completed.Length,
                completed.Sum(answer => answer.WordCount));
        }).ToArray();
    }

    public async Task<WrittenGoalDto> CreateGoalAsync(
        Guid userId,
        WrittenGoalInput input,
        CancellationToken cancellationToken)
    {
        ValidateGoalInput(input);
        var goal = new WrittenGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            TargetSubmissions = input.TargetSubmissions,
            TargetWords = input.TargetWords,
        };
        dbContext.WrittenGoals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetGoalsAsync(userId, cancellationToken)).Single(item => item.Id == goal.Id);
    }

    public async Task<WrittenGoalDto?> UpdateGoalAsync(
        Guid userId,
        Guid goalId,
        WrittenGoalInput input,
        CancellationToken cancellationToken)
    {
        ValidateGoalInput(input);
        var goal = await dbContext.WrittenGoals.SingleOrDefaultAsync(
            candidate => candidate.Id == goalId && candidate.UserId == userId,
            cancellationToken);
        if (goal is null)
        {
            return null;
        }

        goal.StartDate = input.StartDate;
        goal.EndDate = input.EndDate;
        goal.TargetSubmissions = input.TargetSubmissions;
        goal.TargetWords = input.TargetWords;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetGoalsAsync(userId, cancellationToken)).Single(item => item.Id == goal.Id);
    }

    public async Task<WrittenProgressDto> GetProgressAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = from ?? today.AddDays(-29);
        var endDate = to ?? today;
        if (startDate > endDate || endDate.DayNumber - startDate.DayNumber > 366)
        {
            throw Invalid("Progress dates must be ordered and cover no more than one year.");
        }

        var start = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endExclusive = new DateTimeOffset(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var drafts = await dbContext.WrittenAnswers.AsNoTracking()
            .CountAsync(answer =>
                answer.UserId == userId &&
                answer.Status == WrittenAnswerStatus.Draft &&
                answer.UpdatedAt >= start && answer.UpdatedAt < endExclusive,
                cancellationToken);
        var submissions = await dbContext.WrittenAnswerRevisions.AsNoTracking()
            .Where(revision =>
                revision.Answer.UserId == userId &&
                revision.Kind == WrittenAnswerRevisionKind.Submission &&
                revision.CreatedAt >= start && revision.CreatedAt < endExclusive)
            .Include(revision => revision.Answer)
                .ThenInclude(answer => answer.Question)
                .ThenInclude(question => question.SyllabusItem)
            .ToListAsync(cancellationToken);
        var topicPerformance = submissions
            .GroupBy(revision => new
            {
                revision.Answer.Question.SyllabusItemId,
                revision.Answer.Question.SyllabusItem.Title,
            })
            .Select(group =>
            {
                var ratings = group.SelectMany(revision => GetRatings(revision.SelfAssessmentJson)).ToArray();
                return new WrittenTopicProgressDto(
                    group.Key.SyllabusItemId,
                    group.Key.Title,
                    group.Count(),
                    ratings.Length == 0 ? null : (int)Math.Round(ratings.Average()));
            })
            .OrderBy(topic => topic.AverageSelfAssessment ?? 0)
            .ThenByDescending(topic => topic.SubmittedAnswers)
            .Take(20)
            .ToArray();
        return new WrittenProgressDto(
            startDate,
            endDate,
            drafts,
            submissions.Count,
            submissions.Sum(revision => revision.WordCount),
            submissions.Sum(revision => revision.TimeSpentSeconds),
            topicPerformance);
    }

    private async Task<WrittenAnswer?> LoadAnswerAsync(
        Guid userId,
        Guid answerId,
        CancellationToken cancellationToken,
        bool includeRevisions = false)
    {
        IQueryable<WrittenAnswer> query = dbContext.WrittenAnswers
            .Include(answer => answer.Question)
                .ThenInclude(question => question.SyllabusItem);
        if (includeRevisions)
        {
            query = query.Include(answer => answer.Revisions);
        }

        return await query.SingleOrDefaultAsync(
            answer => answer.Id == answerId && answer.UserId == userId,
            cancellationToken);
    }

    private async Task<WrittenAnswerDto> GetAnswerDtoAsync(Guid userId, Guid answerId, CancellationToken cancellationToken)
    {
        var answer = await LoadAnswerAsync(userId, answerId, cancellationToken)
            ?? throw new InvalidOperationException("The saved Written answer could not be loaded.");
        return ToAnswerDto(answer);
    }

    private static void ValidateAnswerInput(UpdateWrittenAnswerRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 50000)
        {
            throw Invalid("Answer content must contain between one and fifty thousand characters.");
        }

        if (request.TimeSpentSeconds is < 0 or > 86400)
        {
            throw Invalid("Writing time must be between zero and 86400 seconds.");
        }

        if (request.SelfAssessment is { } assessment &&
            (assessment.ContentCoverage is < 1 or > 5 ||
             assessment.Organization is < 1 or > 5 ||
             assessment.Clarity is < 1 or > 5 ||
             assessment.LanguageAccuracy is < 1 or > 5 ||
             assessment.Reflection?.Length > 2000 ||
             (assessment.ContentCoverage is null &&
              assessment.Organization is null &&
              assessment.Clarity is null &&
              assessment.LanguageAccuracy is null &&
              string.IsNullOrWhiteSpace(assessment.Reflection))))
        {
            throw Invalid("Self-assessment ratings must be between one and five and include at least one rating or reflection.");
        }
    }

    private static void ApplyAnswerInput(WrittenAnswer answer, UpdateWrittenAnswerRequest request)
    {
        answer.Content = request.Content.Trim();
        answer.WordCount = CountWords(answer.Content);
        answer.TimeSpentSeconds = request.TimeSpentSeconds;
        answer.SelfAssessmentJson = request.SelfAssessment is null
            ? answer.SelfAssessmentJson
            : JsonSerializer.Serialize(request.SelfAssessment, JsonOptions);
        answer.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void AddAnswerRevision(WrittenAnswer answer, WrittenAnswerRevisionKind kind)
    {
        answer.Revisions.Add(new WrittenAnswerRevision
        {
            Id = Guid.NewGuid(),
            AnswerId = answer.Id,
            RevisionNumber = answer.Revisions.Count + 1,
            Kind = kind,
            Content = answer.Content,
            WordCount = answer.WordCount,
            TimeSpentSeconds = answer.TimeSpentSeconds,
            SelfAssessmentJson = answer.SelfAssessmentJson,
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }

    private static void EnsureDraft(WrittenAnswer answer)
    {
        if (answer.Status != WrittenAnswerStatus.Draft)
        {
            throw new WrittenException(
                StatusCodes.Status409Conflict,
                "Answer is submitted",
                "Start a revision before editing a submitted answer.");
        }
    }

    private static int CountWords(string content) =>
        content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static WrittenAnswerDto ToAnswerDto(WrittenAnswer answer) =>
        new(
            answer.Id,
            answer.QuestionId,
            answer.Question.Prompt,
            answer.Question.Type.ToString(),
            answer.Question.SyllabusItemId,
            answer.Question.SyllabusItem.Title,
            answer.Status.ToString(),
            answer.Content,
            answer.WordCount,
            answer.TimeSpentSeconds,
            DeserializeAssessment(answer.SelfAssessmentJson),
            answer.StartedAt,
            answer.UpdatedAt,
            answer.SubmittedAt);

    private static WrittenAnswerListItemDto ToAnswerListItem(WrittenAnswer answer) =>
        new(
            answer.Id,
            answer.QuestionId,
            answer.Question.Prompt,
            answer.Question.Type.ToString(),
            answer.Question.SyllabusItemId,
            answer.Question.SyllabusItem.Title,
            answer.Status.ToString(),
            answer.WordCount,
            answer.StartedAt,
            answer.UpdatedAt,
            answer.SubmittedAt);

    private static WrittenAnswerRevisionDto ToRevisionDto(WrittenAnswerRevision revision) =>
        new(
            revision.RevisionNumber,
            revision.Kind.ToString(),
            revision.Content,
            revision.WordCount,
            revision.TimeSpentSeconds,
            DeserializeAssessment(revision.SelfAssessmentJson),
            revision.CreatedAt);

    private static WrittenSelfAssessmentInput? DeserializeAssessment(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<WrittenSelfAssessmentInput>(json, JsonOptions);

    private static int[] GetRatings(string? json)
    {
        var assessment = DeserializeAssessment(json);
        if (assessment is null)
        {
            return [];
        }

        return new[] { assessment.ContentCoverage, assessment.Organization, assessment.Clarity, assessment.LanguageAccuracy }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
    }

    private static void ValidateGoalInput(WrittenGoalInput input)
    {
        if (input is null ||
            input.StartDate == default ||
            input.EndDate < input.StartDate ||
            input.EndDate.DayNumber - input.StartDate.DayNumber > 366 ||
            input.TargetSubmissions is < 1 or > 100 ||
            input.TargetWords is < 1 or > 1000000)
        {
            throw Invalid("The writing goal has invalid dates or target values.");
        }
    }

    private static WrittenException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Written content not found", detail);
}

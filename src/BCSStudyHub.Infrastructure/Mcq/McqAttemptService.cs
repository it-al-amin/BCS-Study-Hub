using System.Text.Json;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Mcq;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Mcq;

internal sealed partial class McqService
{
    public async Task<McqAttemptDto> CreateAttemptAsync(
        Guid userId,
        Guid examSetId,
        CancellationToken cancellationToken)
    {
        var set = await IncludeExamSetDetails(dbContext.McqExamSets.AsNoTracking())
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == examSetId &&
                candidate.Status == McqContentStatus.Published &&
                candidate.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                candidate.SyllabusVersion.ExamStage.Code == "Preliminary",
                cancellationToken);
        if (set is null)
        {
            throw NotFound("The published Preliminary practice set was not found.");
        }

        var candidateIds = await CandidateQuestions(set)
            .Select(question => question.Id)
            .ToArrayAsync(cancellationToken);
        if (candidateIds.Length < set.QuestionCount)
        {
            throw Invalid($"This set needs {set.QuestionCount} questions, but only {candidateIds.Length} are currently available.");
        }

        Random.Shared.Shuffle(candidateIds);
        var selectedIds = candidateIds.Take(set.QuestionCount).ToArray();
        var selectedRows = await IncludeQuestionDetails(dbContext.McqQuestions.AsNoTracking()
                .Where(question =>
                    selectedIds.Contains(question.Id) &&
                    question.Status == McqContentStatus.Published))
            .ToListAsync(cancellationToken);
        if (selectedRows.Count != selectedIds.Length)
        {
            throw new McqException(
                StatusCodes.Status409Conflict,
                "Practice set changed",
                "The available question set changed while the attempt was being prepared. Please try again.");
        }

        var selectedById = selectedRows.ToDictionary(question => question.Id);
        var selected = selectedIds.Select(id => selectedById[id]).ToArray();
        var startedAt = DateTimeOffset.UtcNow;
        var attempt = new McqAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExamSetId = set.Id,
            ExamSetNameSnapshot = set.Name,
            Mode = set.Mode,
            Status = McqAttemptStatus.InProgress,
            StartedAt = startedAt,
            ExpiresAt = set.DurationSeconds is int seconds ? startedAt.AddSeconds(seconds) : null,
            DurationSeconds = set.DurationSeconds,
            MarksCorrect = set.MarksCorrect,
            MarksIncorrect = set.MarksIncorrect,
            MarksUnanswered = set.MarksUnanswered,
            ShowFeedbackImmediately = set.ShowFeedbackImmediately,
            MaximumScore = set.QuestionCount * set.MarksCorrect,
        };

        for (var index = 0; index < selected.Length; index++)
        {
            var question = selected[index];
            var attemptQuestion = new McqAttemptQuestion
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                QuestionId = question.Id,
                SyllabusItemIdSnapshot = question.SyllabusItemId,
                TopicTitleSnapshot = question.SyllabusItem.Title,
                ParentTopicTitleSnapshot = question.SyllabusItem.Parent?.Title,
                Position = index + 1,
                PromptSnapshot = question.Prompt,
                ExplanationSnapshot = question.Explanation,
                ImageUrlSnapshot = question.ImageUrl,
                Type = question.Type,
                Difficulty = question.Difficulty,
                SourcesSnapshotJson = JsonSerializer.Serialize(
                    question.Sources.OrderBy(source => source.Title).Select(ToSourceDto),
                    JsonOptions),
            };
            foreach (var option in question.Options.OrderBy(option => option.SortOrder))
            {
                attemptQuestion.Options.Add(new McqAttemptOption
                {
                    Id = Guid.NewGuid(),
                    AttemptQuestionId = attemptQuestion.Id,
                    SourceOptionId = option.Id,
                    TextSnapshot = option.Text,
                    SortOrder = option.SortOrder,
                    IsCorrect = option.IsCorrect,
                });
            }

            attempt.Questions.Add(attemptQuestion);
        }

        dbContext.McqAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAttemptDto(attempt);
    }

    public async Task<McqAttemptDto?> GetAttemptAsync(
        Guid userId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, cancellationToken);
        return attempt is null ? null : ToAttemptDto(attempt);
    }

    public async Task<McqAnswerUpdateDto?> UpdateAnswerAsync(
        Guid userId,
        Guid attemptId,
        Guid attemptQuestionId,
        UpdateMcqAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        EnsureAttemptIsOpen(attempt);
        if (request.SelectedOptionIds is null || request.SelectedOptionIds.Count > 8)
        {
            throw Invalid("At most eight options may be selected.");
        }
        if (attempt.ExpiresAt is DateTimeOffset expiresAt && DateTimeOffset.UtcNow >= expiresAt)
        {
            await FinalizeAttemptAsync(attempt, timedOut: true, cancellationToken);
            throw new McqException(
                StatusCodes.Status409Conflict,
                "Attempt time expired",
                "The time limit has elapsed; the attempt was submitted automatically.");
        }

        var question = attempt.Questions.SingleOrDefault(candidate => candidate.Id == attemptQuestionId);
        if (question is null)
        {
            throw NotFound("The question is not part of this attempt.");
        }

        if (request.SelectedOptionIds.Count != request.SelectedOptionIds.Distinct().Count())
        {
            throw Invalid("Selected option identifiers must be unique.");
        }

        var allowedOptionIds = question.Options.Select(option => option.Id).ToHashSet();
        if (request.SelectedOptionIds.Any(id => !allowedOptionIds.Contains(id)))
        {
            throw Invalid("One or more selected options do not belong to this attempt question.");
        }

        if (question.Type == McqQuestionType.SingleAnswer && request.SelectedOptionIds.Count > 1)
        {
            throw Invalid("A single-answer question accepts at most one selected option.");
        }

        var answer = question.Answer;
        if (answer is null)
        {
            answer = new McqAttemptAnswer
            {
                AttemptQuestionId = question.Id,
                AttemptQuestion = question,
            };
            question.Answer = answer;
        }

        answer.SelectedOptionIdsJson = JsonSerializer.Serialize(request.SelectedOptionIds, JsonOptions);
        answer.IsMarkedForReview = request.IsMarkedForReview;
        answer.UpdatedAt = DateTimeOffset.UtcNow;
        var isCorrect = McqScoring.Evaluate(
            request.SelectedOptionIds,
            question.Options.Where(option => option.IsCorrect).Select(option => option.Id).ToArray(),
            attempt.MarksCorrect,
            attempt.MarksIncorrect,
            attempt.MarksUnanswered).IsCorrect ?? false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new McqAnswerUpdateDto(
            question.QuestionId,
            request.SelectedOptionIds,
            request.IsMarkedForReview,
            attempt.ShowFeedbackImmediately && request.SelectedOptionIds.Count > 0 ? isCorrect : null,
            attempt.ShowFeedbackImmediately && request.SelectedOptionIds.Count > 0
                ? question.ExplanationSnapshot
                : null);
    }

    public async Task<McqAttemptDto?> SubmitAttemptAsync(
        Guid userId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        if (attempt.Status == McqAttemptStatus.InProgress)
        {
            var timedOut = attempt.ExpiresAt is DateTimeOffset expiresAt && DateTimeOffset.UtcNow >= expiresAt;
            await FinalizeAttemptAsync(attempt, timedOut, cancellationToken);
        }

        return ToAttemptDto(attempt);
    }

    public async Task<McqAttemptReviewDto?> GetAttemptReviewAsync(
        Guid userId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, cancellationToken);
        if (attempt is null || attempt.Status != McqAttemptStatus.Submitted)
        {
            return null;
        }

        var questions = new List<McqAttemptReviewQuestionDto>(attempt.Questions.Count);
        foreach (var question in attempt.Questions.OrderBy(item => item.Position))
        {
            IReadOnlyList<Guid> selected = question.Answer is null
                ? Array.Empty<Guid>()
                : DeserializeIds(question.Answer.SelectedOptionIdsJson);
            var correct = question.Answer?.IsCorrect == true;
            var sources = JsonSerializer.Deserialize<IReadOnlyList<McqQuestionSourceDto>>(
                question.SourcesSnapshotJson,
                JsonOptions) ?? [];
            questions.Add(new McqAttemptReviewQuestionDto(
                question.QuestionId,
                question.Position,
                question.PromptSnapshot,
                question.ImageUrlSnapshot,
                question.ExplanationSnapshot,
                question.Type.ToString(),
                question.Difficulty.ToString(),
                question.Options.OrderBy(option => option.SortOrder)
                    .Select(option => new McqOptionDto(option.Id, option.TextSnapshot, option.SortOrder, option.IsCorrect))
                    .ToArray(),
                selected,
                question.Answer?.IsMarkedForReview ?? false,
                correct,
                question.Answer?.MarksAwarded ?? attempt.MarksUnanswered,
                question.SyllabusItemIdSnapshot,
                question.TopicTitleSnapshot,
                question.ParentTopicTitleSnapshot,
                sources));
        }

        return new McqAttemptReviewDto(ToAttemptDto(attempt), questions);
    }

    public async Task<PagedResponse<McqAttemptListItemDto>> GetAttemptHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var attempts = dbContext.McqAttempts.AsNoTracking()
            .Where(attempt => attempt.UserId == userId)
            .OrderByDescending(attempt => attempt.StartedAt);
        var total = await attempts.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await attempts.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<McqAttemptListItemDto>(
            rows.Select(ToAttemptListItem).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<PagedResponse<McqQuestionDto>> GetBookmarksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var bookmarks = dbContext.McqBookmarks.AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId &&
                               bookmark.Question.Status == McqContentStatus.Published &&
                               bookmark.Question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published)
            .OrderByDescending(bookmark => bookmark.CreatedAt);
        var total = await bookmarks.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await IncludeQuestionDetails(bookmarks.Select(bookmark => bookmark.Question))
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<McqQuestionDto>(
            rows.Select(question => ToQuestionDto(question, true)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<bool> AddBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.McqQuestions.AnyAsync(
            question => question.Id == questionId &&
                        question.Status == McqContentStatus.Published &&
                        question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published,
            cancellationToken);
        if (!exists)
        {
            throw NotFound("The published question was not found.");
        }

        if (await dbContext.McqBookmarks.AnyAsync(
                bookmark => bookmark.UserId == userId && bookmark.QuestionId == questionId,
                cancellationToken))
        {
            return false;
        }

        dbContext.McqBookmarks.Add(new McqBookmark
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
        var bookmark = await dbContext.McqBookmarks.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.QuestionId == questionId,
            cancellationToken);
        if (bookmark is null)
        {
            return false;
        }

        dbContext.McqBookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResponse<McqMistakeDto>> GetMistakesAsync(
        Guid userId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var mistakes = IncludeMistakeDetails(dbContext.McqMistakes.AsNoTracking())
            .Where(mistake => mistake.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var requestedStatus = ParseEnum<McqMistakeStatus>(status, "mistake status");
            mistakes = mistakes.Where(mistake => mistake.Status == requestedStatus);
        }

        mistakes = mistakes.OrderByDescending(mistake => mistake.UpdatedAt);
        var total = await mistakes.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await mistakes.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<McqMistakeDto>(
            rows.Select(ToMistakeDto).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<McqMistakeDto?> UpdateMistakeAsync(
        Guid userId,
        Guid questionId,
        UpdateMcqMistakeRequest request,
        CancellationToken cancellationToken)
    {
        var mistake = await IncludeMistakeDetails(dbContext.McqMistakes)
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId && candidate.QuestionId == questionId,
                cancellationToken);
        if (mistake is null)
        {
            return null;
        }

        mistake.Status = ParseEnum<McqMistakeStatus>(request.Status, "mistake status");
        mistake.Notes = Normalize(request.Notes);
        mistake.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToMistakeDto(mistake);
    }

    public async Task<McqAnalyticsDto> GetAnalyticsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var completed = dbContext.McqAttempts.AsNoTracking()
            .Where(attempt => attempt.UserId == userId && attempt.Status == McqAttemptStatus.Submitted);
        var summaries = await completed
            .Select(attempt => new
            {
                attempt.Score,
                attempt.CorrectCount,
                attempt.IncorrectCount,
                attempt.UnansweredCount,
            })
            .ToListAsync(cancellationToken);
        var attemptCount = summaries.Count;
        var correct = summaries.Sum(row => (long)row.CorrectCount);
        var incorrect = summaries.Sum(row => (long)row.IncorrectCount);
        var unanswered = summaries.Sum(row => (long)row.UnansweredCount);
        var answerCount = correct + incorrect;
        var topicRows = await dbContext.McqAttemptQuestions.AsNoTracking()
            .Where(question =>
                question.Attempt.UserId == userId &&
                question.Attempt.Status == McqAttemptStatus.Submitted)
            .GroupBy(question => new { question.SyllabusItemIdSnapshot, question.TopicTitleSnapshot })
            .Select(group => new
            {
                group.Key.SyllabusItemIdSnapshot,
                group.Key.TopicTitleSnapshot,
                AnswerCount = group.LongCount(question => question.Answer != null && question.Answer.IsCorrect != null),
                CorrectCount = group.LongCount(question => question.Answer != null && question.Answer.IsCorrect == true),
            })
            .OrderByDescending(group => group.AnswerCount)
            .Take(10)
            .ToListAsync(cancellationToken);
        var recentRows = await completed.OrderByDescending(attempt => attempt.StartedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new McqAnalyticsDto(
            attemptCount,
            answerCount,
            correct,
            incorrect,
            unanswered,
            answerCount == 0 ? 0 : Math.Round((decimal)correct * 100 / answerCount, 2),
            attemptCount == 0
                ? null
                : Math.Round(summaries.Where(row => row.Score.HasValue).Average(row => row.Score!.Value), 2),
            topicRows.Select(row => new McqTopicPerformanceDto(
                row.SyllabusItemIdSnapshot,
                row.TopicTitleSnapshot,
                row.AnswerCount,
                row.CorrectCount,
                row.AnswerCount == 0 ? 0 : Math.Round((decimal)row.CorrectCount * 100 / row.AnswerCount, 2)))
                .ToArray(),
            recentRows.Select(ToAttemptListItem).ToArray());
    }

    private async Task<McqAttempt?> LoadAttemptAsync(
        Guid userId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.McqAttempts
            .Include(attempt => attempt.Questions)
                .ThenInclude(question => question.Options)
            .Include(attempt => attempt.Questions)
                .ThenInclude(question => question.Answer)
            .SingleOrDefaultAsync(
                attempt => attempt.Id == attemptId && attempt.UserId == userId,
                cancellationToken);
        if (attempt is { Status: McqAttemptStatus.InProgress, ExpiresAt: DateTimeOffset expiresAt } &&
            DateTimeOffset.UtcNow >= expiresAt)
        {
            await FinalizeAttemptAsync(attempt, timedOut: true, cancellationToken);
        }

        return attempt;
    }

    private async Task FinalizeAttemptAsync(
        McqAttempt attempt,
        bool timedOut,
        CancellationToken cancellationToken)
    {
        var score = 0m;
        var correctCount = 0;
        var incorrectCount = 0;
        var unansweredCount = 0;
        foreach (var question in attempt.Questions)
        {
            var answer = question.Answer;
            IReadOnlyList<Guid> selected = answer is null
                ? Array.Empty<Guid>()
                : DeserializeIds(answer.SelectedOptionIdsJson);
            var result = McqScoring.Evaluate(
                selected,
                question.Options.Where(option => option.IsCorrect).Select(option => option.Id).ToArray(),
                attempt.MarksCorrect,
                attempt.MarksIncorrect,
                attempt.MarksUnanswered);
            if (result.IsCorrect is null)
            {
                unansweredCount++;
                SetAnswerResult(answer, result);
                score += result.MarksAwarded;
                continue;
            }

            SetAnswerResult(answer, result);
            score += result.MarksAwarded;
            if (result.IsCorrect.Value)
            {
                correctCount++;
            }
            else
            {
                incorrectCount++;
                await RecordMistakeAsync(attempt.UserId, question.QuestionId, attempt.Id, cancellationToken);
            }
        }

        attempt.Status = McqAttemptStatus.Submitted;
        attempt.IsTimedOut = timedOut;
        attempt.SubmittedAt = DateTimeOffset.UtcNow;
        attempt.Score = score;
        attempt.CorrectCount = correctCount;
        attempt.IncorrectCount = incorrectCount;
        attempt.UnansweredCount = unansweredCount;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordMistakeAsync(
        Guid userId,
        Guid questionId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var mistake = await dbContext.McqMistakes.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.QuestionId == questionId,
            cancellationToken);
        if (mistake is null)
        {
            dbContext.McqMistakes.Add(new McqMistake
            {
                UserId = userId,
                QuestionId = questionId,
                LastAttemptId = attemptId,
                Status = McqMistakeStatus.Open,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            mistake.LastAttemptId = attemptId;
            mistake.Status = McqMistakeStatus.Open;
            mistake.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static void SetAnswerResult(McqAttemptAnswer? answer, McqScoreResult result)
    {
        if (answer is null)
        {
            return;
        }

        answer.IsCorrect = result.IsCorrect;
        answer.MarksAwarded = result.MarksAwarded;
        answer.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static McqAttemptDto ToAttemptDto(McqAttempt attempt) =>
        new(
            attempt.Id,
            attempt.ExamSetId,
            attempt.ExamSetNameSnapshot,
            attempt.Mode.ToString(),
            attempt.Status.ToString(),
            attempt.StartedAt,
            attempt.ExpiresAt,
            attempt.SubmittedAt,
            attempt.IsTimedOut,
            attempt.Score,
            attempt.MaximumScore,
            attempt.CorrectCount,
            attempt.IncorrectCount,
            attempt.UnansweredCount,
            attempt.Questions.OrderBy(question => question.Position)
                .Select(question =>
                {
                    IReadOnlyList<Guid> selected = question.Answer is null
                        ? Array.Empty<Guid>()
                        : DeserializeIds(question.Answer.SelectedOptionIdsJson);
                    return new McqAttemptQuestionDto(
                        question.Id,
                        question.QuestionId,
                        question.Position,
                        question.PromptSnapshot,
                        question.ImageUrlSnapshot,
                        question.Type.ToString(),
                        question.Difficulty.ToString(),
                        question.Options.OrderBy(option => option.SortOrder)
                            .Select(option => new McqOptionDto(option.Id, option.TextSnapshot, option.SortOrder))
                            .ToArray(),
                        selected,
                        question.Answer?.IsMarkedForReview ?? false);
                })
                .ToArray());

    private static McqAttemptListItemDto ToAttemptListItem(McqAttempt attempt) =>
        new(
            attempt.Id,
            attempt.ExamSetId,
            attempt.ExamSetNameSnapshot,
            attempt.Mode.ToString(),
            attempt.Status.ToString(),
            attempt.StartedAt,
            attempt.SubmittedAt,
            attempt.IsTimedOut,
            attempt.Score,
            attempt.MaximumScore,
            attempt.CorrectCount,
            attempt.IncorrectCount,
            attempt.UnansweredCount);

    private static IQueryable<McqMistake> IncludeMistakeDetails(IQueryable<McqMistake> mistakes) =>
        mistakes
            .Include(mistake => mistake.Question)
                .ThenInclude(question => question.SyllabusItem)
                    .ThenInclude(item => item.Parent);

    private static McqMistakeDto ToMistakeDto(McqMistake mistake) =>
        new(
            mistake.QuestionId,
            mistake.Question.Prompt,
            mistake.Question.Difficulty.ToString(),
            mistake.Question.SyllabusItemId,
            mistake.Question.SyllabusItem.Title,
            mistake.Question.SyllabusItem.Parent?.Title,
            mistake.Status.ToString(),
            mistake.Notes,
            mistake.LastAttemptId,
            mistake.UpdatedAt);

    private static IReadOnlyList<Guid> DeserializeIds(string json) =>
        JsonSerializer.Deserialize<Guid[]>(json, JsonOptions) ?? [];

    private static void EnsureAttemptIsOpen(McqAttempt attempt)
    {
        if (attempt.Status != McqAttemptStatus.InProgress)
        {
            throw new McqException(
                StatusCodes.Status409Conflict,
                "Attempt already submitted",
                "Answers cannot be changed after an attempt has been submitted.");
        }
    }
}

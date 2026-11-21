using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Planner;

/// <summary>
/// General revision queue and mistake notebook. Scheduling uses only the user's saved <see cref="RevisionPolicy"/>.
/// Sources are validated per type: questions must be published, notes and mistakes must belong to the user.
/// </summary>
internal sealed class RevisionService(
    ApplicationDbContext dbContext,
    PlannerSettingsStore settingsStore,
    StudyMetrics metrics,
    TimeProvider timeProvider) : IRevisionService
{
    private const int TitleLength = 300;
    private const int MaxImport = 500;

    public Task<RevisionPolicy> GetPolicyAsync(Guid userId, CancellationToken cancellationToken) =>
        settingsStore.GetPolicyAsync(userId, cancellationToken);

    public async Task<RevisionQueueDto> GetQueueAsync(
        Guid userId,
        bool dueOnly,
        string? sourceType,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        var today = BangladeshTime.Today(timeProvider);
        var items = dbContext.RevisionItems.AsNoTracking().Include(item => item.SyllabusItem).Where(item => item.UserId == userId);
        var itemStatus = string.IsNullOrWhiteSpace(status) ? RevisionItemStatus.Active : PlannerValidation.Parse<RevisionItemStatus>(status, "status");
        items = items.Where(item => item.Status == itemStatus);
        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            var parsed = PlannerValidation.Parse<RevisionSourceType>(sourceType, "source type");
            items = items.Where(item => item.SourceType == parsed);
        }

        var active = dbContext.RevisionItems.AsNoTracking().Where(item => item.UserId == userId && item.Status == RevisionItemStatus.Active);
        var dueCount = await active.CountAsync(item => item.DueOn <= today, cancellationToken);
        var overdueCount = await active.CountAsync(item => item.DueOn < today, cancellationToken);
        var currentAffairsDue = await dbContext.CurrentAffairsRevisions.AsNoTracking()
            .CountAsync(item => item.UserId == userId && item.NextReviewOn <= today && item.Entry.Status == CurrentAffairsStatus.Published, cancellationToken);

        if (dueOnly)
        {
            items = items.Where(item => item.DueOn <= today);
        }

        items = items.OrderBy(item => item.DueOn).ThenBy(item => item.CreatedAt).ThenBy(item => item.Id);
        var total = await items.LongCountAsync(cancellationToken);
        var page = dueOnly
            ? new PageRequest(1, Math.Min(settings.DailyRevisionLimit, PageRequest.MaximumPageSize))
            : new PageRequest(pageNumber, pageSize);
        var rows = await items.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new RevisionQueueDto(
            rows.Select(item => ToDto(item, today)).ToArray(),
            dueCount,
            overdueCount,
            settings.DailyRevisionLimit,
            currentAffairsDue,
            total,
            page.EffectivePageNumber,
            page.EffectivePageSize);
    }

    public async Task<RevisionItemDto> AddItemAsync(Guid userId, RevisionItemInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var type = PlannerValidation.Parse<RevisionSourceType>(input.SourceType, "source type");
        var today = BangladeshTime.Today(timeProvider);
        if (input.DueOn is DateOnly requested && requested < today)
        {
            throw PlannerValidation.Invalid("The first review date cannot be in the past.");
        }

        if (type != RevisionSourceType.Custom)
        {
            var existing = await dbContext.RevisionItems.Include(item => item.SyllabusItem)
                .FirstOrDefaultAsync(item => item.UserId == userId && item.SourceType == type && item.SourceId == input.SourceId, cancellationToken);
            if (existing is not null)
            {
                return ToDto(existing, today);
            }
        }

        var (title, topicId) = type == RevisionSourceType.Custom
            ? (input.Title!.Trim(), (Guid?)null)
            : await ResolveSourceAsync(userId, type, input.SourceId!.Value, cancellationToken);
        var item = NewItem(userId, type, type == RevisionSourceType.Custom ? null : input.SourceId, title, topicId, ReasonFor(type), input.DueOn ?? today);
        dbContext.RevisionItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDtoAsync(item.Id, today, cancellationToken);
    }

    public async Task<RevisionItemDto?> ReviewAsync(Guid userId, Guid itemId, RevisionReviewInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var outcome = PlannerValidation.Parse<RevisionOutcome>(input.Outcome, "review outcome");
        var item = await dbContext.RevisionItems.SingleOrDefaultAsync(candidate => candidate.Id == itemId && candidate.UserId == userId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (item.Status != RevisionItemStatus.Active)
        {
            throw PlannerValidation.Invalid("Resume this item before reviewing it.");
        }

        var policy = await settingsStore.GetPolicyAsync(userId, cancellationToken);
        var today = BangladeshTime.Today(timeProvider);
        var now = timeProvider.GetUtcNow();
        var before = item.Step;
        var (step, interval, retire) = policy.Next(item.Step, outcome);
        if (outcome == RevisionOutcome.Again && item.ReviewCount > 0)
        {
            item.LapseCount++;
        }

        item.Step = step;
        item.IntervalDays = interval;
        item.DueOn = today.AddDays(interval);
        item.ReviewCount++;
        item.LastOutcome = outcome;
        item.LastReviewedAt = now;
        if (retire)
        {
            item.Status = RevisionItemStatus.Retired;
        }

        dbContext.RevisionLogs.Add(new RevisionLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RevisionItemId = item.Id,
            SourceType = item.SourceType,
            SourceId = item.SourceId,
            Title = item.Title,
            SyllabusItemId = item.SyllabusItemId,
            Outcome = outcome,
            StepBefore = before,
            StepAfter = step,
            IntervalDays = interval,
            ReviewedAt = now,
        });

        if (item.SourceType == RevisionSourceType.Mistake && item.SourceId is Guid mistakeId)
        {
            var mistake = await dbContext.MistakeEntries.SingleOrDefaultAsync(entry => entry.Id == mistakeId && entry.UserId == userId, cancellationToken);
            if (mistake is not null)
            {
                mistake.ReviewOn = retire ? null : item.DueOn;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDtoAsync(item.Id, today, cancellationToken);
    }

    public async Task<RevisionItemDto?> SetItemStatusAsync(Guid userId, Guid itemId, string status, CancellationToken cancellationToken)
    {
        var item = await dbContext.RevisionItems.SingleOrDefaultAsync(candidate => candidate.Id == itemId && candidate.UserId == userId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var today = BangladeshTime.Today(timeProvider);
        var newStatus = PlannerValidation.Parse<RevisionItemStatus>(status, "status");
        if (newStatus == RevisionItemStatus.Active && item.Status != RevisionItemStatus.Active && item.DueOn < today)
        {
            item.DueOn = today;
        }

        item.Status = newStatus;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDtoAsync(item.Id, today, cancellationToken);
    }

    public async Task<bool> DeleteItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken) =>
        await dbContext.RevisionItems.Where(item => item.Id == itemId && item.UserId == userId).ExecuteDeleteSafeAsync(dbContext, cancellationToken);

    public async Task<RevisionImportResultDto> ImportAsync(Guid userId, RevisionImportInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var source = input.Source.Trim();
        List<(RevisionSourceType Type, Guid Id, string Title, Guid? TopicId, RevisionReason Reason)> candidates = source.ToLowerInvariant() switch
        {
            "mcqbookmarks" => (await dbContext.McqBookmarks.AsNoTracking()
                    .Where(bookmark => bookmark.UserId == userId && bookmark.Question.Status == McqContentStatus.Published)
                    .Select(bookmark => new { bookmark.QuestionId, bookmark.Question.Prompt, bookmark.Question.SyllabusItemId }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.McqQuestion, row.QuestionId, Clip(row.Prompt), (Guid?)row.SyllabusItemId, RevisionReason.Bookmark)).ToList(),
            "writtenbookmarks" => (await dbContext.WrittenBookmarks.AsNoTracking()
                    .Where(bookmark => bookmark.UserId == userId && bookmark.Question.Status == WrittenQuestionStatus.Published)
                    .Select(bookmark => new { bookmark.QuestionId, bookmark.Question.Prompt, bookmark.Question.SyllabusItemId }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.WrittenQuestion, row.QuestionId, Clip(row.Prompt), (Guid?)row.SyllabusItemId, RevisionReason.Bookmark)).ToList(),
            "vivabookmarks" => (await dbContext.VivaBookmarks.AsNoTracking()
                    .Where(bookmark => bookmark.UserId == userId && bookmark.Question.Status == VivaQuestionStatus.Published)
                    .Select(bookmark => new { bookmark.QuestionId, bookmark.Question.Prompt }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.VivaQuestion, row.QuestionId, Clip(row.Prompt), (Guid?)null, RevisionReason.Bookmark)).ToList(),
            "mcqmistakes" => (await dbContext.McqMistakes.AsNoTracking()
                    .Where(mistake => mistake.UserId == userId && mistake.Status != McqMistakeStatus.Resolved && mistake.Question.Status == McqContentStatus.Published)
                    .Select(mistake => new { mistake.QuestionId, mistake.Question.Prompt, mistake.Question.SyllabusItemId }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.McqQuestion, row.QuestionId, Clip(row.Prompt), (Guid?)row.SyllabusItemId, RevisionReason.Mistake)).ToList(),
            "mistakenotebook" => (await dbContext.MistakeEntries.AsNoTracking()
                    .Where(mistake => mistake.UserId == userId && mistake.Status == MistakeStatus.Open)
                    .Select(mistake => new { mistake.Id, mistake.QuestionText, mistake.SyllabusItemId }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.Mistake, row.Id, Clip(row.QuestionText), row.SyllabusItemId, RevisionReason.Mistake)).ToList(),
            "weaktopics" => (await GetWeakTopicsAsync(userId, cancellationToken))
                .Select(topic => (RevisionSourceType.Topic, topic.SyllabusItemId, Clip(topic.Title), (Guid?)topic.SyllabusItemId, RevisionReason.WeakTopic)).ToList(),
            "writtennotes" => (await dbContext.WrittenStudyNotes.AsNoTracking().Where(note => note.UserId == userId)
                    .Select(note => new { note.Id, note.Title, note.SyllabusItemId }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.WrittenNote, row.Id, Clip(row.Title), (Guid?)row.SyllabusItemId, RevisionReason.Note)).ToList(),
            "librarynotes" => (await dbContext.LibraryPersonalNotes.AsNoTracking().Where(note => note.UserId == userId)
                    .Select(note => new { note.Id, note.Resource.Title, note.Content }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.LibraryNote, row.Id, Clip($"{row.Title}: {row.Content}"), (Guid?)null, RevisionReason.Note)).ToList(),
            "vivanotes" => (await dbContext.VivaAnswerNotes.AsNoTracking().Where(note => note.UserId == userId)
                    .Select(note => new { note.Id, note.Question.Prompt }).Take(MaxImport).ToListAsync(cancellationToken))
                .Select(row => (RevisionSourceType.VivaNote, row.Id, Clip(row.Prompt), (Guid?)null, RevisionReason.Note)).ToList(),
            _ => throw PlannerValidation.Invalid("Choose McqBookmarks, WrittenBookmarks, VivaBookmarks, McqMistakes, MistakeNotebook, WeakTopics, WrittenNotes, LibraryNotes, or VivaNotes."),
        };

        var types = candidates.Select(candidate => candidate.Type).Distinct().ToArray();
        var existing = (await dbContext.RevisionItems.AsNoTracking()
                .Where(item => item.UserId == userId && types.Contains(item.SourceType) && item.SourceId != null)
                .Select(item => new { item.SourceType, SourceId = item.SourceId!.Value }).ToListAsync(cancellationToken))
            .Select(item => (item.SourceType, item.SourceId)).ToHashSet();
        var today = BangladeshTime.Today(timeProvider);
        var added = 0;
        foreach (var candidate in candidates.DistinctBy(candidate => (candidate.Type, candidate.Id)))
        {
            if (existing.Contains((candidate.Type, candidate.Id)))
            {
                continue;
            }

            dbContext.RevisionItems.Add(NewItem(userId, candidate.Type, candidate.Id, candidate.Title, candidate.TopicId, candidate.Reason, today));
            added++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new RevisionImportResultDto(source, added, candidates.Count - added);
    }

    public async Task<PagedResponse<RevisionLogDto>> GetHistoryAsync(Guid userId, DateOnly? from, DateOnly? to, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var logs = dbContext.RevisionLogs.AsNoTracking().Where(log => log.UserId == userId);
        if (from is DateOnly fromDate)
        {
            var start = BangladeshTime.StartOf(fromDate);
            logs = logs.Where(log => log.ReviewedAt >= start);
        }

        if (to is DateOnly toDate)
        {
            var end = BangladeshTime.StartOf(toDate.AddDays(1));
            logs = logs.Where(log => log.ReviewedAt < end);
        }

        logs = logs.OrderByDescending(log => log.ReviewedAt).ThenBy(log => log.Id);
        var total = await logs.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await logs.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<RevisionLogDto>(rows.Select(ToLogDto).ToArray(), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    /// <summary>
    /// Weak topics use the user's thresholds: MCQ accuracy below the configured percentage over at least the configured
    /// number of answers, or a self-rated mastery of 1–2. Open mistakes are reported as supporting evidence.
    /// </summary>
    public async Task<IReadOnlyList<WeakTopicDto>> GetWeakTopicsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        var answers = (await metrics.McqAnswersAsync(userId, null, null, cancellationToken))
            .GroupBy(answer => answer.SyllabusItemId)
            .ToDictionary(group => group.Key, group => (Answered: group.Sum(answer => answer.Answered), Correct: group.Sum(answer => answer.Correct)));
        var mastery = await dbContext.TopicProgress.AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.Mastery != null)
            .Select(progress => new { progress.SyllabusItemId, Mastery = progress.Mastery!.Value })
            .ToDictionaryAsync(progress => progress.SyllabusItemId, progress => progress.Mastery, cancellationToken);
        var mistakes = (await dbContext.MistakeEntries.AsNoTracking()
                .Where(mistake => mistake.UserId == userId && mistake.Status == MistakeStatus.Open && mistake.SyllabusItemId != null)
                .Select(mistake => mistake.SyllabusItemId!.Value).ToListAsync(cancellationToken))
            .GroupBy(id => id).ToDictionary(group => group.Key, group => group.Count());

        var weak = new HashSet<Guid>();
        weak.UnionWith(answers.Where(pair => pair.Value.Answered >= settings.WeakTopicMinimumAnswers &&
                                              PlannerRules.AccuracyPercent(pair.Value.Correct, pair.Value.Answered) < settings.WeakTopicAccuracyPercent)
            .Select(pair => pair.Key));
        weak.UnionWith(mastery.Where(pair => pair.Value <= 2).Select(pair => pair.Key));
        var topics = await metrics.TopicsAsync(weak, cancellationToken);
        return weak.Where(topics.ContainsKey).Select(id =>
            {
                var topic = topics[id];
                var (answered, correct) = answers.GetValueOrDefault(id);
                var accuracy = PlannerRules.AccuracyPercent(correct, answered);
                var reasons = new List<string>();
                if (answered >= settings.WeakTopicMinimumAnswers && accuracy < settings.WeakTopicAccuracyPercent)
                {
                    reasons.Add($"MCQ accuracy {accuracy}% over {answered} answers (threshold {settings.WeakTopicAccuracyPercent}%)");
                }

                if (mastery.TryGetValue(id, out var level) && level <= 2)
                {
                    reasons.Add($"Self-rated mastery {level}/5");
                }

                if (mistakes.TryGetValue(id, out var open))
                {
                    reasons.Add($"{open} open mistake{(open == 1 ? string.Empty : "s")}");
                }

                return new WeakTopicDto(id, topic.Title, topic.SubjectName, topic.StageCode, answered, correct, accuracy,
                    mastery.TryGetValue(id, out var value) ? value : null, mistakes.GetValueOrDefault(id), reasons);
            })
            .OrderBy(topic => topic.AccuracyPercent ?? 100)
            .ThenBy(topic => topic.Mastery ?? 5)
            .Take(50)
            .ToArray();
    }

    // ---------- Mistake notebook ----------

    public async Task<PagedResponse<MistakeDto>> GetMistakesAsync(Guid userId, MistakeQuery query, CancellationToken cancellationToken)
    {
        var mistakes = dbContext.MistakeEntries.AsNoTracking().Include(mistake => mistake.SyllabusItem).Where(mistake => mistake.UserId == userId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            mistakes = mistakes.Where(mistake => mistake.QuestionText.Contains(search) ||
                                                 (mistake.Explanation != null && mistake.Explanation.Contains(search)) ||
                                                 mistake.CorrectAnswerReference.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = PlannerValidation.Parse<MistakeStatus>(query.Status, "status");
            mistakes = mistakes.Where(mistake => mistake.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = PlannerValidation.Parse<MistakeCategory>(query.Category, "category");
            mistakes = mistakes.Where(mistake => mistake.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            var sourceType = PlannerValidation.Parse<MistakeSourceType>(query.SourceType, "source");
            mistakes = mistakes.Where(mistake => mistake.SourceType == sourceType);
        }

        if (query.SyllabusItemId is Guid topicId)
        {
            mistakes = mistakes.Where(mistake => mistake.SyllabusItemId == topicId);
        }

        mistakes = mistakes.OrderByDescending(mistake => mistake.UpdatedAt).ThenBy(mistake => mistake.Id);
        var total = await mistakes.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await mistakes.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(row => row.Id).ToArray();
        var revisionIds = await dbContext.RevisionItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.SourceType == RevisionSourceType.Mistake && item.SourceId != null && ids.Contains(item.SourceId.Value))
            .ToDictionaryAsync(item => item.SourceId!.Value, item => item.Id, cancellationToken);
        return new PagedResponse<MistakeDto>(
            rows.Select(row => ToMistakeDto(row, revisionIds.TryGetValue(row.Id, out var revisionId) ? revisionId : null, [])).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<MistakeDto?> GetMistakeAsync(Guid userId, Guid mistakeId, CancellationToken cancellationToken)
    {
        var mistake = await dbContext.MistakeEntries.AsNoTracking().Include(entry => entry.SyllabusItem)
            .SingleOrDefaultAsync(entry => entry.Id == mistakeId && entry.UserId == userId, cancellationToken);
        if (mistake is null)
        {
            return null;
        }

        var revisionId = await dbContext.RevisionItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.SourceType == RevisionSourceType.Mistake && item.SourceId == mistakeId)
            .Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);
        var history = await dbContext.RevisionLogs.AsNoTracking()
            .Where(log => log.UserId == userId && log.SourceType == RevisionSourceType.Mistake && log.SourceId == mistakeId)
            .OrderByDescending(log => log.ReviewedAt).Take(100).ToListAsync(cancellationToken);
        return ToMistakeDto(mistake, revisionId, history.Select(ToLogDto).ToArray());
    }

    public async Task<MistakeDto> CreateMistakeAsync(Guid userId, MistakeInput input, CancellationToken cancellationToken)
    {
        var mistake = new MistakeEntry { Id = Guid.NewGuid(), UserId = userId };
        await ApplyMistakeAsync(mistake, input, cancellationToken);
        dbContext.MistakeEntries.Add(mistake);
        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        if (settings.AutoQueueMistakes || mistake.ReviewOn is not null)
        {
            var today = BangladeshTime.Today(timeProvider);
            dbContext.RevisionItems.Add(NewItem(userId, RevisionSourceType.Mistake, mistake.Id, Clip(mistake.QuestionText), mistake.SyllabusItemId,
                RevisionReason.Mistake, mistake.ReviewOn is DateOnly reviewOn && reviewOn > today ? reviewOn : today));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetMistakeAsync(userId, mistake.Id, cancellationToken))!;
    }

    public async Task<MistakeDto?> UpdateMistakeAsync(Guid userId, Guid mistakeId, MistakeInput input, CancellationToken cancellationToken)
    {
        var mistake = await dbContext.MistakeEntries.SingleOrDefaultAsync(entry => entry.Id == mistakeId && entry.UserId == userId, cancellationToken);
        if (mistake is null)
        {
            return null;
        }

        await ApplyMistakeAsync(mistake, input, cancellationToken);
        var item = await dbContext.RevisionItems.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.SourceType == RevisionSourceType.Mistake && candidate.SourceId == mistakeId, cancellationToken);
        if (item is not null)
        {
            item.Title = Clip(mistake.QuestionText);
            item.SyllabusItemId = mistake.SyllabusItemId;
            if (mistake.ReviewOn is DateOnly reviewOn)
            {
                item.DueOn = reviewOn;
            }
        }
        else if (mistake.ReviewOn is DateOnly reviewOn)
        {
            dbContext.RevisionItems.Add(NewItem(userId, RevisionSourceType.Mistake, mistake.Id, Clip(mistake.QuestionText), mistake.SyllabusItemId, RevisionReason.Mistake, reviewOn));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetMistakeAsync(userId, mistakeId, cancellationToken);
    }

    public async Task<MistakeDto?> SetMistakeStatusAsync(Guid userId, Guid mistakeId, string status, CancellationToken cancellationToken)
    {
        var mistake = await dbContext.MistakeEntries.SingleOrDefaultAsync(entry => entry.Id == mistakeId && entry.UserId == userId, cancellationToken);
        if (mistake is null)
        {
            return null;
        }

        mistake.Status = PlannerValidation.Parse<MistakeStatus>(status, "status");
        mistake.ResolvedAt = mistake.Status == MistakeStatus.Resolved ? timeProvider.GetUtcNow() : null;
        var item = await dbContext.RevisionItems.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.SourceType == RevisionSourceType.Mistake && candidate.SourceId == mistakeId, cancellationToken);
        if (item is not null)
        {
            var today = BangladeshTime.Today(timeProvider);
            item.Status = mistake.Status == MistakeStatus.Resolved ? RevisionItemStatus.Retired : RevisionItemStatus.Active;
            if (item.Status == RevisionItemStatus.Active && item.DueOn < today)
            {
                item.DueOn = today;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetMistakeAsync(userId, mistakeId, cancellationToken);
    }

    public async Task<bool> DeleteMistakeAsync(Guid userId, Guid mistakeId, CancellationToken cancellationToken)
    {
        var mistake = await dbContext.MistakeEntries.SingleOrDefaultAsync(entry => entry.Id == mistakeId && entry.UserId == userId, cancellationToken);
        if (mistake is null)
        {
            return false;
        }

        var items = await dbContext.RevisionItems
            .Where(item => item.UserId == userId && item.SourceType == RevisionSourceType.Mistake && item.SourceId == mistakeId)
            .ToListAsync(cancellationToken);
        dbContext.RevisionItems.RemoveRange(items);
        dbContext.MistakeEntries.Remove(mistake);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Copies unresolved MCQ mistakes into the notebook with the question text and correct option(s) as reference.</summary>
    public async Task<int> ImportMcqMistakesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var already = (await dbContext.MistakeEntries.AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.SourceType == MistakeSourceType.Mcq && entry.QuestionId != null)
            .Select(entry => entry.QuestionId!.Value).ToListAsync(cancellationToken)).ToHashSet();
        var rows = await dbContext.McqMistakes.AsNoTracking()
            .Where(mistake => mistake.UserId == userId && mistake.Status != McqMistakeStatus.Resolved)
            .Select(mistake => new
            {
                mistake.QuestionId,
                mistake.Question.Prompt,
                mistake.Question.Explanation,
                mistake.Question.SyllabusItemId,
                mistake.Notes,
                Correct = mistake.Question.Options.Where(option => option.IsCorrect).OrderBy(option => option.SortOrder).Select(option => option.Text).ToList(),
            })
            .Take(MaxImport)
            .ToListAsync(cancellationToken);
        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        var today = BangladeshTime.Today(timeProvider);
        var created = 0;
        foreach (var row in rows.Where(row => !already.Contains(row.QuestionId)))
        {
            var mistake = new MistakeEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceType = MistakeSourceType.Mcq,
                QuestionId = row.QuestionId,
                QuestionText = row.Prompt,
                CorrectAnswerReference = row.Correct.Count == 0 ? "See the question's explanation." : string.Join("; ", row.Correct),
                Explanation = row.Notes ?? row.Explanation,
                Category = MistakeCategory.Concept,
                SyllabusItemId = row.SyllabusItemId,
            };
            dbContext.MistakeEntries.Add(mistake);
            if (settings.AutoQueueMistakes)
            {
                dbContext.RevisionItems.Add(NewItem(userId, RevisionSourceType.Mistake, mistake.Id, Clip(mistake.QuestionText), mistake.SyllabusItemId, RevisionReason.Mistake, today));
            }

            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task ApplyMistakeAsync(MistakeEntry mistake, MistakeInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var type = PlannerValidation.Parse<MistakeSourceType>(input.SourceType, "source");
        Guid? questionTopic = null;
        if (input.QuestionId is Guid questionId)
        {
            questionTopic = type switch
            {
                MistakeSourceType.Mcq => await dbContext.McqQuestions.Where(question => question.Id == questionId).Select(question => (Guid?)question.SyllabusItemId).SingleOrDefaultAsync(cancellationToken)
                    ?? throw PlannerValidation.Invalid("The MCQ question was not found."),
                MistakeSourceType.Written => await dbContext.WrittenQuestions.Where(question => question.Id == questionId).Select(question => (Guid?)question.SyllabusItemId).SingleOrDefaultAsync(cancellationToken)
                    ?? throw PlannerValidation.Invalid("The Written question was not found."),
                MistakeSourceType.Viva => await dbContext.VivaQuestions.AnyAsync(question => question.Id == questionId, cancellationToken)
                    ? null
                    : throw PlannerValidation.Invalid("The Viva question was not found."),
                _ => throw PlannerValidation.Invalid("Mistakes of type 'Other' are not linked to a question."),
            };
        }

        var topicId = input.SyllabusItemId ?? questionTopic;
        if (topicId is Guid id && !await dbContext.SyllabusItems.AnyAsync(item => item.Id == id && item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published, cancellationToken))
        {
            throw PlannerValidation.Invalid("The topic must belong to a published syllabus version.");
        }

        mistake.SourceType = type;
        mistake.QuestionId = input.QuestionId;
        mistake.QuestionText = input.QuestionText.Trim();
        mistake.YourAnswer = PlannerValidation.Normalize(input.YourAnswer);
        mistake.CorrectAnswerReference = input.CorrectAnswerReference.Trim();
        mistake.Explanation = PlannerValidation.Normalize(input.Explanation);
        mistake.Category = PlannerValidation.Parse<MistakeCategory>(input.Category, "category");
        mistake.SyllabusItemId = topicId;
        mistake.ReviewOn = input.ReviewOn;
    }

    private async Task<(string Title, Guid? TopicId)> ResolveSourceAsync(Guid userId, RevisionSourceType type, Guid id, CancellationToken cancellationToken)
    {
        object? found = type switch
        {
            RevisionSourceType.Topic => await dbContext.SyllabusItems.AsNoTracking()
                .Where(item => item.Id == id && item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published)
                .Select(item => new Resolved(item.Title, item.Id)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.McqQuestion => await dbContext.McqQuestions.AsNoTracking()
                .Where(question => question.Id == id && question.Status == McqContentStatus.Published)
                .Select(question => new Resolved(question.Prompt, question.SyllabusItemId)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.WrittenQuestion => await dbContext.WrittenQuestions.AsNoTracking()
                .Where(question => question.Id == id && question.Status == WrittenQuestionStatus.Published)
                .Select(question => new Resolved(question.Prompt, question.SyllabusItemId)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.VivaQuestion => await dbContext.VivaQuestions.AsNoTracking()
                .Where(question => question.Id == id && question.Status == VivaQuestionStatus.Published)
                .Select(question => new Resolved(question.Prompt, null)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.Mistake => await dbContext.MistakeEntries.AsNoTracking()
                .Where(mistake => mistake.Id == id && mistake.UserId == userId)
                .Select(mistake => new Resolved(mistake.QuestionText, mistake.SyllabusItemId)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.WrittenNote => await dbContext.WrittenStudyNotes.AsNoTracking()
                .Where(note => note.Id == id && note.UserId == userId)
                .Select(note => new Resolved(note.Title, note.SyllabusItemId)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.LibraryNote => await dbContext.LibraryPersonalNotes.AsNoTracking()
                .Where(note => note.Id == id && note.UserId == userId)
                .Select(note => new Resolved(note.Resource.Title + ": " + note.Content, null)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.VivaNote => await dbContext.VivaAnswerNotes.AsNoTracking()
                .Where(note => note.Id == id && note.UserId == userId)
                .Select(note => new Resolved(note.Question.Prompt, null)).SingleOrDefaultAsync(cancellationToken),
            RevisionSourceType.CurrentAffairs => throw PlannerValidation.Invalid("Add current-affairs entries to revision from the current-affairs page; they share your revision settings."),
            _ => throw PlannerValidation.Invalid("The revision source type is not supported."),
        };

        return found is Resolved resolved
            ? (Clip(resolved.Title), resolved.TopicId)
            : throw new PlannerException(StatusCodes.Status404NotFound, "Not found", "The record to revise was not found, is not published, or is not yours.");
    }

    private sealed record Resolved(string Title, Guid? TopicId);

    private static RevisionReason ReasonFor(RevisionSourceType type) => type switch
    {
        RevisionSourceType.Mistake => RevisionReason.Mistake,
        RevisionSourceType.WrittenNote or RevisionSourceType.LibraryNote or RevisionSourceType.VivaNote => RevisionReason.Note,
        _ => RevisionReason.Manual,
    };

    private RevisionItem NewItem(Guid userId, RevisionSourceType type, Guid? sourceId, string title, Guid? topicId, RevisionReason reason, DateOnly dueOn) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceType = type,
            SourceId = sourceId,
            Title = title,
            SyllabusItemId = topicId,
            Reason = reason,
            DueOn = dueOn,
            CreatedAt = timeProvider.GetUtcNow(),
        };

    private async Task<RevisionItemDto> GetDtoAsync(Guid itemId, DateOnly today, CancellationToken cancellationToken)
    {
        var item = await dbContext.RevisionItems.AsNoTracking().Include(candidate => candidate.SyllabusItem).SingleAsync(candidate => candidate.Id == itemId, cancellationToken);
        return ToDto(item, today);
    }

    private static RevisionItemDto ToDto(RevisionItem item, DateOnly today) =>
        new(item.Id, item.SourceType.ToString(), item.SourceId, item.Title, item.SyllabusItemId, item.SyllabusItem?.Title, item.Reason.ToString(),
            item.Status.ToString(), item.Step, item.IntervalDays, item.DueOn, item.Status == RevisionItemStatus.Active && item.DueOn <= today,
            item.ReviewCount, item.LapseCount, item.LastOutcome?.ToString(), item.LastReviewedAt);

    internal static RevisionLogDto ToLogDto(RevisionLog log) =>
        new(log.Id, log.SourceType.ToString(), log.SourceId, log.Title, log.Outcome.ToString(), log.StepBefore, log.StepAfter, log.IntervalDays, log.ReviewedAt);

    private static MistakeDto ToMistakeDto(MistakeEntry mistake, Guid? revisionItemId, IReadOnlyList<RevisionLogDto> history) =>
        new(mistake.Id, mistake.SourceType.ToString(), mistake.QuestionId, mistake.QuestionText, mistake.YourAnswer, mistake.CorrectAnswerReference,
            mistake.Explanation, mistake.Category.ToString(), mistake.Status.ToString(), mistake.SyllabusItemId, mistake.SyllabusItem?.Title,
            mistake.ReviewOn, revisionItemId, history, mistake.CreatedAt, mistake.UpdatedAt, mistake.ResolvedAt);

    private static string Clip(string value)
    {
        var text = value.ReplaceLineEndings(" ").Trim();
        return text.Length <= TitleLength ? text : text[..(TitleLength - 1)].TrimEnd() + "…";
    }
}

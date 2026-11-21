using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.CurrentAffairs;

/// <summary>
/// Curated current affairs. Learners (anonymous included) see only published entries; personal bookmarks, notes,
/// and revision state are owner-scoped; editing requires <c>currentaffairs.manage</c>. Nothing is scraped:
/// every entry is written by an editor and must cite a source that the editor marked verified before publishing.
/// </summary>
internal sealed partial class CurrentAffairsService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    BCSStudyHub.Infrastructure.Planner.PlannerSettingsStore settingsStore) : ICurrentAffairsService
{
    private const int MaxDigestEntries = 1000;
    private const int PromptPreviewLength = 300;

    public async Task<PagedResponse<CurrentAffairsSummaryDto>> GetEntriesAsync(
        CurrentAffairsQuery query,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var entries = Published();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length is < 2 or > 100)
            {
                throw Invalid("Search text must contain between two and one hundred characters.");
            }

            entries = entries.Where(entry =>
                entry.Title.Contains(search) ||
                entry.Summary.Contains(search) ||
                entry.SourceName.Contains(search) ||
                entry.Tags.Any(tag => tag.Tag == search));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = Parse<CurrentAffairsCategory>(query.Category, "category");
            entries = entries.Where(entry => entry.Category == category);
        }

        if (query.From is DateOnly from && query.To is DateOnly to && to < from)
        {
            throw Invalid("The end date cannot be before the start date.");
        }

        if (query.From is DateOnly fromDate)
        {
            entries = entries.Where(entry => entry.EventDate >= fromDate);
        }

        if (query.To is DateOnly toDate)
        {
            entries = entries.Where(entry => entry.EventDate <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tag = query.Tag.Trim();
            entries = entries.Where(entry => entry.Tags.Any(item => item.Tag == tag));
        }

        if (query.SubjectId is Guid subjectId)
        {
            entries = entries.Where(entry =>
                entry.Subjects.Any(subject => subject.SubjectId == subjectId) ||
                entry.Topics.Any(topic => topic.SyllabusItem.SyllabusSubject.SubjectId == subjectId));
        }

        if (query.SyllabusItemId is Guid itemId)
        {
            entries = entries.Where(entry =>
                entry.Topics.Any(topic => topic.SyllabusItemId == itemId || topic.SyllabusItem.ParentId == itemId));
        }

        if (query.BookmarkedOnly)
        {
            if (userId is not Guid currentUserId)
            {
                throw new CurrentAffairsException(StatusCodes.Status401Unauthorized, "Authentication required", "Sign in to filter bookmarked entries.");
            }

            entries = entries.Where(entry => dbContext.CurrentAffairsBookmarks.Any(bookmark =>
                bookmark.UserId == currentUserId && bookmark.EntryId == entry.Id));
        }

        entries = (query.SortBy ?? "recent").ToLowerInvariant() switch
        {
            "recent" => entries.OrderByDescending(entry => entry.EventDate).ThenByDescending(entry => entry.PublishedAt).ThenBy(entry => entry.Id),
            "oldest" => entries.OrderBy(entry => entry.EventDate).ThenBy(entry => entry.Id),
            "updated" => entries.OrderByDescending(entry => entry.UpdatedAt).ThenBy(entry => entry.Id),
            _ => throw Invalid("SortBy must be one of: recent, oldest, updated."),
        };

        return await ToPageAsync(entries, query.PageNumber, query.PageSize, userId, cancellationToken);
    }

    public async Task<CurrentAffairsDetailDto?> GetEntryAsync(Guid entryId, Guid? userId, CancellationToken cancellationToken)
    {
        var entry = await WithDetailIncludes(Published().Where(candidate => candidate.Id == entryId))
            .SingleOrDefaultAsync(cancellationToken);
        return entry is null ? null : await ToDetailAsync(entry, userId, adminView: false, cancellationToken);
    }

    public async Task<CurrentAffairsDigestDto> GetDigestAsync(
        string period,
        DateOnly? anchor,
        string? category,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var digestPeriod = Parse<DigestPeriod>(period, "digest period");
        var anchorDate = anchor ?? CurrentAffairsRules.Today(timeProvider);
        var (from, to) = CurrentAffairsRules.GetRange(digestPeriod, anchorDate);
        var entries = Published().Where(entry => entry.EventDate >= from && entry.EventDate <= to);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var parsed = Parse<CurrentAffairsCategory>(category, "category");
            entries = entries.Where(entry => entry.Category == parsed);
        }

        var total = await entries.CountAsync(cancellationToken);
        var rows = await WithSummaryIncludes(entries
                .OrderByDescending(entry => entry.EventDate)
                .ThenBy(entry => entry.Title)
                .Take(MaxDigestEntries))
            .ToListAsync(cancellationToken);
        var summaries = await ToSummariesAsync(rows, userId, cancellationToken);
        var groups = summaries
            .GroupBy(summary => Enum.Parse<CurrentAffairsCategory>(summary.Category))
            .OrderBy(group => group.Key)
            .Select(group => new CurrentAffairsDigestGroupDto(group.Key.ToString(), group.ToArray()))
            .ToArray();
        return new CurrentAffairsDigestDto(
            digestPeriod.ToString(),
            CurrentAffairsRules.Label(digestPeriod, anchorDate),
            from,
            to,
            CurrentAffairsRules.Shift(digestPeriod, from, -1),
            CurrentAffairsRules.Shift(digestPeriod, from, 1),
            total,
            groups);
    }

    public async Task<IReadOnlyList<CurrentAffairsArchiveMonthDto>> GetArchiveAsync(CancellationToken cancellationToken)
    {
        // Dates are grouped in memory to avoid relying on provider-specific DateOnly member translation.
        var dates = await Published().Select(entry => entry.EventDate).ToListAsync(cancellationToken);
        return dates
            .GroupBy(date => (date.Year, date.Month))
            .OrderByDescending(group => group.Key.Year)
            .ThenByDescending(group => group.Key.Month)
            .Select(group => new CurrentAffairsArchiveMonthDto(group.Key.Year, group.Key.Month, group.Count()))
            .ToArray();
    }

    public async Task<IReadOnlyList<CurrentAffairsCategoryCountDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var counts = await Published()
            .GroupBy(entry => entry.Category)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return Enum.GetValues<CurrentAffairsCategory>()
            .Select(category => new CurrentAffairsCategoryCountDto(
                category.ToString(),
                counts.Where(count => count.Key == category).Sum(count => count.Count)))
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken)
    {
        var published = Published();
        return await dbContext.CurrentAffairsEntryTags.AsNoTracking()
            .Where(tag => published.Any(entry => entry.Id == tag.EntryId))
            .Select(tag => tag.Tag)
            .Distinct()
            .OrderBy(tag => tag)
            .Take(300)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<CurrentAffairsEntry> Published() =>
        dbContext.CurrentAffairsEntries.AsNoTracking().Where(entry => entry.Status == CurrentAffairsStatus.Published);

    private static IQueryable<CurrentAffairsEntry> WithSummaryIncludes(IQueryable<CurrentAffairsEntry> entries) =>
        entries
            .Include(entry => entry.Tags)
            .Include(entry => entry.Subjects).ThenInclude(link => link.Subject)
            .Include(entry => entry.Topics).ThenInclude(link => link.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject).ThenInclude(subject => subject.Subject)
            .Include(entry => entry.Topics).ThenInclude(link => link.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject).ThenInclude(subject => subject.SyllabusVersion)
                .ThenInclude(version => version.ExamStage)
            .AsSplitQuery();

    private static IQueryable<CurrentAffairsEntry> WithDetailIncludes(IQueryable<CurrentAffairsEntry> entries) =>
        WithSummaryIncludes(entries)
            .Include(entry => entry.Questions)
            .Include(entry => entry.References)
            .Include(entry => entry.OfficialSource)
            .Include(entry => entry.LibraryResources).ThenInclude(link => link.Resource).ThenInclude(resource => resource.Files);

    private async Task<PagedResponse<CurrentAffairsSummaryDto>> ToPageAsync(
        IQueryable<CurrentAffairsEntry> entries,
        int pageNumber,
        int pageSize,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var total = await entries.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await WithSummaryIncludes(entries.Skip(page.Offset).Take(page.EffectivePageSize)).ToListAsync(cancellationToken);
        return new PagedResponse<CurrentAffairsSummaryDto>(
            await ToSummariesAsync(rows, userId, cancellationToken),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    private async Task<IReadOnlyList<CurrentAffairsSummaryDto>> ToSummariesAsync(
        IReadOnlyList<CurrentAffairsEntry> rows,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        HashSet<Guid> bookmarked = [], revising = [], noted = [];
        if (userId is Guid currentUserId)
        {
            var ids = rows.Select(row => row.Id).ToArray();
            bookmarked = (await dbContext.CurrentAffairsBookmarks.AsNoTracking()
                .Where(item => item.UserId == currentUserId && ids.Contains(item.EntryId))
                .Select(item => item.EntryId).ToListAsync(cancellationToken)).ToHashSet();
            revising = (await dbContext.CurrentAffairsRevisions.AsNoTracking()
                .Where(item => item.UserId == currentUserId && ids.Contains(item.EntryId))
                .Select(item => item.EntryId).ToListAsync(cancellationToken)).ToHashSet();
            noted = (await dbContext.CurrentAffairsStudyNotes.AsNoTracking()
                .Where(item => item.UserId == currentUserId && ids.Contains(item.EntryId))
                .Select(item => item.EntryId).ToListAsync(cancellationToken)).ToHashSet();
        }

        return rows.Select(row => ToSummary(row, bookmarked.Contains(row.Id), revising.Contains(row.Id), noted.Contains(row.Id))).ToArray();
    }

    private async Task<CurrentAffairsDetailDto> ToDetailAsync(
        CurrentAffairsEntry entry,
        Guid? userId,
        bool adminView,
        CancellationToken cancellationToken)
    {
        var summary = (await ToSummariesAsync([entry], userId, cancellationToken))[0];
        var questions = await LoadQuestionRefsAsync(entry.Questions, cancellationToken);
        if (!adminView)
        {
            questions = questions.Where(question => question.IsAvailable).ToArray();
        }

        var resources = entry.LibraryResources
            .Select(link => new CurrentAffairsResourceRefDto(
                link.ResourceId,
                link.Resource.Title,
                link.Resource.Kind.ToString(),
                link.Resource.Files.FirstOrDefault(file => file.Role == LibraryFileRole.Primary)?.Format.ToString(),
                link.Resource.Visibility == LibraryVisibility.Shared && link.Resource.Status == LibraryResourceStatus.Published))
            .Where(resource => adminView || resource.IsAvailable)
            .OrderBy(resource => resource.Title)
            .ToArray();
        var officialSource = entry.OfficialSource is { } source && (adminView || source.Status == OfficialSourceStatus.Published)
            ? new OfficialSourceRefDto(source.Id, source.Name, source.Url, source.VerificationStatus.ToString(), source.LastVerifiedOn)
            : null;

        CurrentAffairsNoteDto? note = null;
        CurrentAffairsRevisionDto? revision = null;
        if (userId is Guid currentUserId)
        {
            var noteRow = await dbContext.CurrentAffairsStudyNotes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == currentUserId && item.EntryId == entry.Id, cancellationToken);
            note = noteRow is null ? null : ToNoteDto(noteRow, entry);
            var revisionRow = await dbContext.CurrentAffairsRevisions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == currentUserId && item.EntryId == entry.Id, cancellationToken);
            revision = revisionRow is null ? null : ToRevisionDto(revisionRow, entry, CurrentAffairsRules.Today(timeProvider));
        }

        return new CurrentAffairsDetailDto(
            summary,
            entry.Content,
            entry.Notes,
            officialSource,
            questions,
            entry.References.OrderBy(reference => reference.Title)
                .Select(reference => new CurrentAffairsReferenceDto(reference.Id, reference.Kind.ToString(), reference.Title, reference.Url))
                .ToArray(),
            resources,
            note,
            revision,
            entry.CreatedAt);
    }

    private async Task<IReadOnlyList<CurrentAffairsQuestionRefDto>> LoadQuestionRefsAsync(
        IEnumerable<CurrentAffairsEntryQuestion> links,
        CancellationToken cancellationToken)
    {
        var linkList = links.ToList();
        if (linkList.Count == 0)
        {
            return [];
        }

        var mcqIds = linkList.Where(link => link.QuestionType == CurrentAffairsQuestionType.Mcq).Select(link => link.QuestionId).ToArray();
        var writtenIds = linkList.Where(link => link.QuestionType == CurrentAffairsQuestionType.Written).Select(link => link.QuestionId).ToArray();
        var vivaIds = linkList.Where(link => link.QuestionType == CurrentAffairsQuestionType.Viva).Select(link => link.QuestionId).ToArray();

        var result = new List<CurrentAffairsQuestionRefDto>();
        if (mcqIds.Length > 0)
        {
            var rows = await dbContext.McqQuestions.AsNoTracking()
                .Where(question => mcqIds.Contains(question.Id))
                .Select(question => new { question.Id, question.Prompt, question.Status })
                .ToListAsync(cancellationToken);
            result.AddRange(rows.Select(row => new CurrentAffairsQuestionRefDto(
                nameof(CurrentAffairsQuestionType.Mcq), row.Id, Preview(row.Prompt), row.Status.ToString(), row.Status == McqContentStatus.Published)));
        }

        if (writtenIds.Length > 0)
        {
            var rows = await dbContext.WrittenQuestions.AsNoTracking()
                .Where(question => writtenIds.Contains(question.Id))
                .Select(question => new { question.Id, question.Prompt, question.Status, VersionStatus = question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status })
                .ToListAsync(cancellationToken);
            result.AddRange(rows.Select(row => new CurrentAffairsQuestionRefDto(
                nameof(CurrentAffairsQuestionType.Written), row.Id, Preview(row.Prompt), row.Status.ToString(),
                row.Status == WrittenQuestionStatus.Published && row.VersionStatus == SyllabusVersionStatus.Published)));
        }

        if (vivaIds.Length > 0)
        {
            var rows = await dbContext.VivaQuestions.AsNoTracking()
                .Where(question => vivaIds.Contains(question.Id))
                .Select(question => new { question.Id, question.Prompt, question.Status })
                .ToListAsync(cancellationToken);
            result.AddRange(rows.Select(row => new CurrentAffairsQuestionRefDto(
                nameof(CurrentAffairsQuestionType.Viva), row.Id, Preview(row.Prompt), row.Status.ToString(), row.Status == VivaQuestionStatus.Published)));
        }

        return result;
    }

    private static CurrentAffairsSummaryDto ToSummary(CurrentAffairsEntry entry, bool bookmarked, bool inRevision, bool hasNote) =>
        new(
            entry.Id,
            entry.Title,
            entry.Summary,
            entry.EventDate,
            entry.SourcePublishedOn,
            entry.Category.ToString(),
            entry.Status.ToString(),
            entry.SourceName,
            entry.SourceUrl,
            entry.SourceStatus.ToString(),
            entry.Tags.Select(tag => tag.Tag).OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
            entry.Subjects.Select(link => new CurrentAffairsSubjectRefDto(link.SubjectId, link.Subject.Code, link.Subject.Name)).OrderBy(subject => subject.Name).ToArray(),
            entry.Topics.Select(link => new CurrentAffairsTopicRefDto(
                    link.SyllabusItemId,
                    link.SyllabusItem.Title,
                    link.SyllabusItem.SyllabusSubject.Subject.Name,
                    link.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code,
                    link.SyllabusItem.SyllabusSubject.SyllabusVersion.VersionLabel))
                .OrderBy(topic => topic.Title)
                .ToArray(),
            bookmarked,
            inRevision,
            hasNote,
            entry.UpdatedAt,
            entry.PublishedAt);

    private static CurrentAffairsNoteDto ToNoteDto(CurrentAffairsStudyNote note, CurrentAffairsEntry entry) =>
        new(note.EntryId, entry.Title, entry.EventDate, note.Content, note.UpdatedAt);

    private static CurrentAffairsRevisionDto ToRevisionDto(CurrentAffairsRevision revision, CurrentAffairsEntry entry, DateOnly today) =>
        new(
            revision.EntryId,
            entry.Title,
            entry.Category.ToString(),
            entry.EventDate,
            revision.ReviewCount,
            revision.IntervalDays,
            revision.NextReviewOn,
            revision.LastReviewedAt,
            revision.LastOutcome?.ToString(),
            revision.NextReviewOn <= today);

    private static string Preview(string prompt) =>
        prompt.Length <= PromptPreviewLength ? prompt : prompt[..PromptPreviewLength].TrimEnd() + "…";

    private void AddAudit(Guid actorId, string action, string entityType, Guid entityId) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier,
        });

    private static void ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            throw Invalid(results[0].ErrorMessage ?? "The request is not valid.");
        }
    }

    internal static T Parse<T>(string? value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _)
            ? parsed
            : throw Invalid($"The requested {field} is not supported.");

    internal static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static CurrentAffairsException Invalid(string message) =>
        new(StatusCodes.Status400BadRequest, "Invalid current-affairs request", message);

    internal static CurrentAffairsException Conflict(string message) =>
        new(StatusCodes.Status409Conflict, "Conflict", message);
}

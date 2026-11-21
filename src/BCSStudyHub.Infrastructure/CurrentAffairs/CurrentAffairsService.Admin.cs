using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.CurrentAffairs;

internal sealed partial class CurrentAffairsService
{
    public async Task<PagedResponse<CurrentAffairsSummaryDto>> GetAdminEntriesAsync(
        string? search,
        string? status,
        string? category,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var entries = dbContext.CurrentAffairsEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            entries = entries.Where(entry => entry.Title.Contains(value) || entry.Summary.Contains(value) || entry.SourceName.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = Parse<CurrentAffairsStatus>(status, "status");
            entries = entries.Where(entry => entry.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var parsed = Parse<CurrentAffairsCategory>(category, "category");
            entries = entries.Where(entry => entry.Category == parsed);
        }

        entries = entries.OrderByDescending(entry => entry.UpdatedAt).ThenBy(entry => entry.Id);
        return await ToPageAsync(entries, pageNumber, pageSize, null, cancellationToken);
    }

    public async Task<CurrentAffairsDetailDto?> GetAdminEntryAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await WithDetailIncludes(dbContext.CurrentAffairsEntries.AsNoTracking().Where(candidate => candidate.Id == entryId))
            .SingleOrDefaultAsync(cancellationToken);
        return entry is null ? null : await ToDetailAsync(entry, null, adminView: true, cancellationToken);
    }

    public async Task<CurrentAffairsDetailDto> CreateEntryAsync(CurrentAffairsEntryInput input, Guid actorId, CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var entry = new CurrentAffairsEntry { Id = Guid.NewGuid(), CreatedById = actorId, Status = CurrentAffairsStatus.Draft };
        await ApplyInputAsync(entry, input, cancellationToken);
        dbContext.CurrentAffairsEntries.Add(entry);
        AddAudit(actorId, "currentaffairs.entry.created", nameof(CurrentAffairsEntry), entry.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAdminEntryAsync(entry.Id, cancellationToken))!;
    }

    public async Task<CurrentAffairsDetailDto?> UpdateEntryAsync(Guid entryId, CurrentAffairsEntryInput input, Guid actorId, CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var entry = await dbContext.CurrentAffairsEntries
            .Include(candidate => candidate.Tags)
            .Include(candidate => candidate.Subjects)
            .Include(candidate => candidate.Topics)
            .Include(candidate => candidate.Questions)
            .Include(candidate => candidate.References)
            .Include(candidate => candidate.LibraryResources)
            .AsSplitQuery()
            .SingleOrDefaultAsync(candidate => candidate.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        await ApplyInputAsync(entry, input, cancellationToken);
        if (entry.Status == CurrentAffairsStatus.Published && ValidateCanPublish(entry) is { } error)
        {
            throw Invalid(error);
        }

        entry.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorId, "currentaffairs.entry.updated", nameof(CurrentAffairsEntry), entry.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminEntryAsync(entry.Id, cancellationToken);
    }

    public async Task<CurrentAffairsDetailDto?> SetStatusAsync(Guid entryId, string status, Guid actorId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.CurrentAffairsEntries.SingleOrDefaultAsync(candidate => candidate.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        var newStatus = Parse<CurrentAffairsStatus>(status, "status");
        if (newStatus == CurrentAffairsStatus.Draft)
        {
            throw Invalid("Entries can only be published or archived.");
        }

        if (newStatus == CurrentAffairsStatus.Published)
        {
            if (ValidateCanPublish(entry) is { } error)
            {
                throw Invalid(error);
            }

            entry.PublishedAt ??= DateTimeOffset.UtcNow;
        }

        entry.Status = newStatus;
        AddAudit(actorId, $"currentaffairs.entry.{newStatus.ToString().ToLowerInvariant()}", nameof(CurrentAffairsEntry), entry.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminEntryAsync(entry.Id, cancellationToken);
    }

    public async Task<bool> DeleteEntryAsync(Guid entryId, Guid actorId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.CurrentAffairsEntries.SingleOrDefaultAsync(candidate => candidate.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        if (entry.Status == CurrentAffairsStatus.Published)
        {
            throw Invalid("Archive a published entry before deleting it.");
        }

        dbContext.CurrentAffairsEntries.Remove(entry);
        AddAudit(actorId, "currentaffairs.entry.deleted", nameof(CurrentAffairsEntry), entry.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CurrentAffairsQuestionSearchDto>> SearchQuestionsAsync(string? type, string search, CancellationToken cancellationToken)
    {
        var value = search?.Trim() ?? string.Empty;
        if (value.Length is < 2 or > 100)
        {
            throw Invalid("Search text must contain between two and one hundred characters.");
        }

        CurrentAffairsQuestionType? filter = string.IsNullOrWhiteSpace(type) ? null : Parse<CurrentAffairsQuestionType>(type, "question type");
        var results = new List<CurrentAffairsQuestionSearchDto>();
        if (filter is null or CurrentAffairsQuestionType.Mcq)
        {
            var rows = await dbContext.McqQuestions.AsNoTracking().Where(question => question.Prompt.Contains(value))
                .OrderByDescending(question => question.UpdatedAt).Take(20)
                .Select(question => new { question.Id, question.Prompt, question.Status }).ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new CurrentAffairsQuestionSearchDto("Mcq", row.Id, Preview(row.Prompt), row.Status.ToString())));
        }

        if (filter is null or CurrentAffairsQuestionType.Written)
        {
            var rows = await dbContext.WrittenQuestions.AsNoTracking().Where(question => question.Prompt.Contains(value))
                .OrderByDescending(question => question.UpdatedAt).Take(20)
                .Select(question => new { question.Id, question.Prompt, question.Status }).ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new CurrentAffairsQuestionSearchDto("Written", row.Id, Preview(row.Prompt), row.Status.ToString())));
        }

        if (filter is null or CurrentAffairsQuestionType.Viva)
        {
            var rows = await dbContext.VivaQuestions.AsNoTracking().Where(question => question.Prompt.Contains(value))
                .OrderByDescending(question => question.UpdatedAt).Take(20)
                .Select(question => new { question.Id, question.Prompt, question.Status }).ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new CurrentAffairsQuestionSearchDto("Viva", row.Id, Preview(row.Prompt), row.Status.ToString())));
        }

        return results;
    }

    public async Task<IReadOnlyList<CurrentAffairsResourceSearchDto>> SearchLibraryResourcesAsync(string? search, CancellationToken cancellationToken)
    {
        var resources = SharedPublishedResources();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            resources = resources.Where(resource => resource.Title.Contains(value));
        }

        var rows = await resources.Include(resource => resource.Files)
            .OrderBy(resource => resource.Title).Take(30).ToListAsync(cancellationToken);
        return rows.Select(resource => new CurrentAffairsResourceSearchDto(
                resource.Id,
                resource.Title,
                resource.Kind.ToString(),
                resource.Files.FirstOrDefault(file => file.Role == LibraryFileRole.Primary)?.Format.ToString()))
            .ToArray();
    }

    /// <summary>Only shared, published library resources may be attached, so a learner's private upload can never surface here.</summary>
    private IQueryable<LibraryResource> SharedPublishedResources() =>
        dbContext.LibraryResources.AsNoTracking().Where(resource =>
            resource.Visibility == LibraryVisibility.Shared && resource.Status == LibraryResourceStatus.Published);

    internal static string? ValidateCanPublish(CurrentAffairsEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SourceName) || !LibraryRules.IsSafeWebUrl(entry.SourceUrl))
        {
            return "Publishing requires a named source with a valid http or https URL.";
        }

        return entry.SourceStatus == CurrentAffairsSourceStatus.Verified
            ? null
            : "Check the cited source and mark it verified before publishing.";
    }

    private async Task ApplyInputAsync(CurrentAffairsEntry entry, CurrentAffairsEntryInput input, CancellationToken cancellationToken)
    {
        if (input.OfficialSourceId is Guid sourceId &&
            !await dbContext.OfficialSources.AnyAsync(source => source.Id == sourceId, cancellationToken))
        {
            throw Invalid("The linked official source does not exist in the directory.");
        }

        var subjectIds = input.SubjectIds.ToHashSet();
        if (subjectIds.Count > 0 &&
            await dbContext.Subjects.CountAsync(subject => subjectIds.Contains(subject.Id) && subject.IsActive, cancellationToken) != subjectIds.Count)
        {
            throw Invalid("Each subject must be an active subject in the syllabus catalogue.");
        }

        var topicIds = input.SyllabusItemIds.ToHashSet();
        if (topicIds.Count > 0 &&
            await dbContext.SyllabusItems.CountAsync(item => topicIds.Contains(item.Id) &&
                                                             item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published,
                cancellationToken) != topicIds.Count)
        {
            throw Invalid("Each topic must belong to a published syllabus version.");
        }

        var resourceIds = input.LibraryResourceIds.ToHashSet();
        var existingResourceIds = entry.LibraryResources.Select(link => link.ResourceId).ToHashSet();
        var newResourceIds = resourceIds.Where(id => !existingResourceIds.Contains(id)).ToArray();
        if (newResourceIds.Length > 0 &&
            await SharedPublishedResources().CountAsync(resource => newResourceIds.Contains(resource.Id), cancellationToken) != newResourceIds.Length)
        {
            throw Invalid("Only shared, published library resources can be attached.");
        }

        var questions = input.Questions
            .Select(question => (Type: Parse<CurrentAffairsQuestionType>(question.Type, "question type"), question.QuestionId))
            .ToHashSet();
        await ValidateQuestionsExistAsync(questions, cancellationToken);

        entry.Title = input.Title.Trim();
        entry.Summary = input.Summary.Trim();
        entry.Content = Normalize(input.Content);
        entry.EventDate = input.EventDate;
        entry.SourcePublishedOn = input.SourcePublishedOn;
        entry.Category = Parse<CurrentAffairsCategory>(input.Category, "category");
        entry.SourceName = input.SourceName.Trim();
        entry.SourceUrl = input.SourceUrl.Trim();
        entry.OfficialSourceId = input.OfficialSourceId;
        entry.SourceStatus = Parse<CurrentAffairsSourceStatus>(input.SourceStatus, "source status");
        entry.Notes = Normalize(input.Notes);

        LibraryRules.TryNormalizeTags(input.Tags, out var tags);
        var tagSet = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in entry.Tags.Where(tag => !tagSet.Contains(tag.Tag)).ToArray())
        {
            entry.Tags.Remove(existing);
        }

        foreach (var tag in tags.Where(tag => entry.Tags.All(existing => !string.Equals(existing.Tag, tag, StringComparison.OrdinalIgnoreCase))))
        {
            entry.Tags.Add(new CurrentAffairsEntryTag { EntryId = entry.Id, Tag = tag });
        }

        Sync(entry.Subjects, subjectIds, link => link.SubjectId, id => new CurrentAffairsEntrySubject { EntryId = entry.Id, SubjectId = id });
        Sync(entry.Topics, topicIds, link => link.SyllabusItemId, id => new CurrentAffairsEntryTopic { EntryId = entry.Id, SyllabusItemId = id });
        Sync(entry.LibraryResources, resourceIds, link => link.ResourceId, id => new CurrentAffairsEntryResource { EntryId = entry.Id, ResourceId = id });
        Sync(entry.Questions, questions, link => (link.QuestionType, link.QuestionId),
            key => new CurrentAffairsEntryQuestion { EntryId = entry.Id, QuestionType = key.Type, QuestionId = key.QuestionId });

        foreach (var reference in entry.References.ToArray())
        {
            entry.References.Remove(reference);
        }

        foreach (var reference in input.References)
        {
            entry.References.Add(new CurrentAffairsEntryReference
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                Kind = Parse<CurrentAffairsReferenceKind>(reference.Kind, "reference kind"),
                Title = reference.Title.Trim(),
                Url = reference.Url.Trim(),
            });
        }
    }

    private async Task ValidateQuestionsExistAsync(
        IReadOnlySet<(CurrentAffairsQuestionType Type, Guid QuestionId)> questions,
        CancellationToken cancellationToken)
    {
        var mcq = questions.Where(item => item.Type == CurrentAffairsQuestionType.Mcq).Select(item => item.QuestionId).ToArray();
        var written = questions.Where(item => item.Type == CurrentAffairsQuestionType.Written).Select(item => item.QuestionId).ToArray();
        var viva = questions.Where(item => item.Type == CurrentAffairsQuestionType.Viva).Select(item => item.QuestionId).ToArray();
        var found =
            (mcq.Length == 0 ? 0 : await dbContext.McqQuestions.CountAsync(question => mcq.Contains(question.Id), cancellationToken)) +
            (written.Length == 0 ? 0 : await dbContext.WrittenQuestions.CountAsync(question => written.Contains(question.Id), cancellationToken)) +
            (viva.Length == 0 ? 0 : await dbContext.VivaQuestions.CountAsync(question => viva.Contains(question.Id), cancellationToken));
        if (found != questions.Count)
        {
            throw Invalid("Each related question must exist in the MCQ, Written, or Viva question bank of the stated type.");
        }
    }

    private static void Sync<TLink, TKey>(ICollection<TLink> links, IReadOnlySet<TKey> desired, Func<TLink, TKey> key, Func<TKey, TLink> create)
    {
        foreach (var link in links.Where(link => !desired.Contains(key(link))).ToArray())
        {
            links.Remove(link);
        }

        var existing = links.Select(key).ToHashSet();
        foreach (var id in desired.Where(id => !existing.Contains(id)))
        {
            links.Add(create(id));
        }
    }
}

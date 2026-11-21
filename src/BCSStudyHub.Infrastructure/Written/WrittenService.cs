using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Written;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Written;

internal sealed partial class WrittenService : IWrittenService
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;

    public WrittenService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<WrittenQuestionDto>> GetQuestionsAsync(
        WrittenQuestionQuery query,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var questions = PublishedWrittenQuestions();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length is < 2 or > 100)
            {
                throw Invalid("Search text must contain between two and one hundred characters.");
            }

            questions = questions.Where(question =>
                question.Prompt.Contains(search) ||
                (question.ExamReference != null && question.ExamReference.Contains(search)));
        }

        if (query.SyllabusVersionId is Guid versionId)
        {
            questions = questions.Where(question =>
                question.SyllabusItem.SyllabusSubject.SyllabusVersionId == versionId);
        }

        if (query.SubjectId is Guid subjectId)
        {
            questions = questions.Where(question =>
                question.SyllabusItem.SyllabusSubject.SubjectId == subjectId);
        }

        if (query.SyllabusItemId is Guid itemId)
        {
            questions = questions.Where(question =>
                question.SyllabusItemId == itemId || question.SyllabusItem.ParentId == itemId);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = ParseEnum<WrittenQuestionType>(query.Type, "question type");
            questions = questions.Where(question => question.Type == type);
        }

        if (query.BcsExamYear is int examYear)
        {
            if (examYear is < 1900 or > 2200)
            {
                throw Invalid("BCS exam year must be between 1900 and 2200.");
            }

            questions = questions.Where(question => question.BcsExamYear == examYear);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tag = query.Tag.Trim();
            if (tag.Length > 60)
            {
                throw Invalid("Question tag cannot exceed 60 characters.");
            }

            questions = questions.Where(question => question.Tags.Any(item => item.Tag == tag));
        }

        if (query.BookmarkedOnly)
        {
            if (userId is not Guid currentUserId)
            {
                throw new WrittenException(
                    StatusCodes.Status401Unauthorized,
                    "Authentication required",
                    "Sign in to filter bookmarked Written questions.");
            }

            questions = questions.Where(question => dbContext.WrittenBookmarks.Any(bookmark =>
                bookmark.UserId == currentUserId && bookmark.QuestionId == question.Id));
        }

        questions = query.SortBy.ToLowerInvariant() switch
        {
            "recent" => questions.OrderByDescending(question => question.CreatedAt).ThenBy(question => question.Id),
            "year" => questions.OrderByDescending(question => question.BcsExamYear).ThenBy(question => question.Id),
            "subject" => questions.OrderBy(question => question.SyllabusItem.SyllabusSubject.Subject.Name)
                .ThenBy(question => question.Id),
            _ => throw Invalid("SortBy must be one of: recent, year, subject."),
        };

        var total = await questions.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await IncludeQuestionDetails(questions)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        var bookmarkedIds = userId is Guid authenticatedUserId
            ? await dbContext.WrittenBookmarks.AsNoTracking()
                .Where(bookmark => bookmark.UserId == authenticatedUserId &&
                                   rows.Select(row => row.Id).Contains(bookmark.QuestionId))
                .Select(bookmark => bookmark.QuestionId)
                .ToHashSetAsync(cancellationToken)
            : [];
        return new PagedResponse<WrittenQuestionDto>(
            rows.Select(question => ToQuestionDto(question, bookmarkedIds.Contains(question.Id))).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<WrittenQuestionDto?> GetQuestionAsync(Guid id, Guid? userId, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(PublishedWrittenQuestions().Where(candidate => candidate.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        if (question is null)
        {
            return null;
        }

        var bookmarked = userId is Guid currentUserId && await dbContext.WrittenBookmarks.AnyAsync(
            bookmark => bookmark.UserId == currentUserId && bookmark.QuestionId == id,
            cancellationToken);
        return ToQuestionDto(question, bookmarked);
    }

    public async Task<WrittenModelAnswerDto?> GetModelAnswerAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var answer = await dbContext.WrittenModelAnswers.AsNoTracking()
            .Include(modelAnswer => modelAnswer.References)
            .SingleOrDefaultAsync(
                modelAnswer => modelAnswer.QuestionId == questionId &&
                               modelAnswer.Status == WrittenModelAnswerStatus.Published &&
                               modelAnswer.Question.Status == WrittenQuestionStatus.Published &&
                               modelAnswer.Question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                               modelAnswer.Question.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Written",
                cancellationToken);
        return answer is null
            ? null
            : new WrittenModelAnswerDto(
                answer.QuestionId,
                answer.Content,
                answer.References.OrderBy(reference => reference.Title).Select(ToSourceDto).ToArray());
    }

    public async Task<PagedResponse<WrittenAdminQuestionDto>> GetAdminQuestionsAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var questions = dbContext.WrittenQuestions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            questions = questions.Where(question =>
                question.Prompt.Contains(value) ||
                (question.ExamReference != null && question.ExamReference.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            questions = questions.Where(question => question.Status == ParseEnum<WrittenQuestionStatus>(status, "status"));
        }

        questions = questions.OrderByDescending(question => question.UpdatedAt).ThenBy(question => question.Id);
        var total = await questions.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await IncludeQuestionDetails(questions)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<WrittenAdminQuestionDto>(
            rows.Select(question => ToAdminQuestionDto(question)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<WrittenAdminQuestionDto?> GetAdminQuestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.WrittenQuestions.AsNoTracking())
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return question is null ? null : ToAdminQuestionDto(question);
    }

    public async Task<WrittenAdminQuestionDto> CreateQuestionAsync(
        WrittenQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateQuestionInput(input);
        await ValidateQuestionItemAsync(input.SyllabusItemId, cancellationToken);
        var question = new WrittenQuestion
        {
            Id = Guid.NewGuid(),
            SyllabusItemId = input.SyllabusItemId,
            CreatedById = actorId,
        };
        ApplyQuestionInput(question, input, actorId);
        dbContext.WrittenQuestions.Add(question);
        AddAudit(actorId, "written.question.created", nameof(WrittenQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminQuestionDtoAsync(question.Id, cancellationToken);
    }

    public async Task<WrittenAdminQuestionDto?> UpdateQuestionAsync(
        Guid id,
        WrittenQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateQuestionInput(input);
        var question = await IncludeQuestionDetails(dbContext.WrittenQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (question is null || question.Status == WrittenQuestionStatus.Archived)
        {
            return null;
        }

        await ValidateQuestionItemAsync(input.SyllabusItemId, cancellationToken);
        question.SyllabusItemId = input.SyllabusItemId;
        ApplyQuestionInput(question, input, actorId);
        if (question.Status == WrittenQuestionStatus.Published)
        {
            await ValidateQuestionCanPublishAsync(question, cancellationToken);
        }

        AddAudit(actorId, "written.question.updated", nameof(WrittenQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminQuestionDtoAsync(question.Id, cancellationToken);
    }

    public async Task<WrittenAdminQuestionDto?> SetQuestionStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.WrittenQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        var newStatus = ParseEnum<WrittenQuestionStatus>(status, "question status");
        if (newStatus == WrittenQuestionStatus.Published)
        {
            await ValidateQuestionCanPublishAsync(question, cancellationToken);
        }
        else if (newStatus == WrittenQuestionStatus.Draft && question.Status != WrittenQuestionStatus.Draft)
        {
            throw Invalid("Published or archived questions cannot return to draft.");
        }

        question.Status = newStatus;
        AddAudit(actorId, $"written.question.{status.ToLowerInvariant()}", nameof(WrittenQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminQuestionDto(question);
    }

    public async Task<WrittenAdminQuestionDto?> SetModelAnswerStatusAsync(
        Guid questionId,
        string status,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.WrittenQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == questionId, cancellationToken);
        if (question?.ModelAnswer is null)
        {
            return null;
        }

        var newStatus = ParseEnum<WrittenModelAnswerStatus>(status, "model-answer status");
        if (newStatus == WrittenModelAnswerStatus.Published)
        {
            if (question.Status != WrittenQuestionStatus.Published ||
                question.ModelAnswer.References.Count == 0 ||
                question.ModelAnswer.References.Any(reference => reference.VerificationStatus != WrittenReferenceStatus.Verified))
            {
                throw Invalid("Publishing a model answer requires a published question and verified references.");
            }
        }
        else if (newStatus == WrittenModelAnswerStatus.Draft &&
                 question.ModelAnswer.Status != WrittenModelAnswerStatus.Draft)
        {
            throw Invalid("Published or archived model answers cannot return to draft.");
        }

        question.ModelAnswer.Status = newStatus;
        question.ModelAnswer.UpdatedById = actorId;
        question.ModelAnswer.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorId, $"written.model-answer.{status.ToLowerInvariant()}", nameof(WrittenModelAnswer), question.ModelAnswer.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminQuestionDto(question);
    }

    private IQueryable<WrittenQuestion> PublishedWrittenQuestions() =>
        dbContext.WrittenQuestions.AsNoTracking()
            .Where(question =>
                question.Status == WrittenQuestionStatus.Published &&
                question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                question.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Written");

    private static IQueryable<WrittenQuestion> IncludeQuestionDetails(IQueryable<WrittenQuestion> questions) =>
        questions
            .Include(question => question.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject)
                    .ThenInclude(mapping => mapping.Subject)
            .Include(question => question.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject)
                    .ThenInclude(mapping => mapping.SyllabusVersion)
            .Include(question => question.SyllabusItem)
                .ThenInclude(item => item.Parent)
            .Include(question => question.SyllabusItem)
                .ThenInclude(item => item.Paper)
            .Include(question => question.Sources)
            .Include(question => question.Resources)
            .Include(question => question.Tags)
            .Include(question => question.ModelAnswer)
                .ThenInclude(answer => answer!.References);

    private async Task ValidateQuestionItemAsync(Guid syllabusItemId, CancellationToken cancellationToken)
    {
        var item = await dbContext.SyllabusItems.AsNoTracking()
            .Where(candidate => candidate.Id == syllabusItemId)
            .Select(candidate => new
            {
                candidate.Kind,
                StageCode = candidate.SyllabusSubject.SyllabusVersion.ExamStage.Code,
                VersionStatus = candidate.SyllabusSubject.SyllabusVersion.Status,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null ||
            item.StageCode != "Written" ||
            item.VersionStatus != SyllabusVersionStatus.Published ||
            item.Kind is not (SyllabusItemKind.Topic or SyllabusItemKind.Subtopic))
        {
            throw Invalid("Written questions must map to a topic or subtopic in a published Written syllabus version.");
        }
    }

    private async Task ValidateQuestionCanPublishAsync(WrittenQuestion question, CancellationToken cancellationToken)
    {
        if (question.Sources.Count == 0 ||
            question.Sources.Any(source => source.VerificationStatus != WrittenReferenceStatus.Verified))
        {
            throw Invalid("Publishing requires at least one verified question source.");
        }

        var itemIsVerified = await dbContext.SyllabusItems.AnyAsync(
            item => item.Id == question.SyllabusItemId &&
                    item.VerificationStatus == SyllabusVerificationStatus.Verified &&
                    item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                    item.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Written",
            cancellationToken);
        if (!itemIsVerified)
        {
            throw Invalid("Publishing requires a verified topic or subtopic in a published Written syllabus version.");
        }
    }

    private static void ValidateQuestionInput(WrittenQuestionInput input)
    {
        if (input is null ||
            string.IsNullOrWhiteSpace(input.Prompt) ||
            input.Sources is null ||
            input.Resources is null ||
            input.Tags is null ||
            input.ModelAnswerReferences is null)
        {
            throw Invalid("Question prompt, source list, resource list, tag list, and model-answer references are required.");
        }

        _ = ParseEnum<WrittenQuestionType>(input.Type, "question type");
        if (input.Sources.Count is < 1 or > 10 || input.Sources.Any(source => source is null))
        {
            throw Invalid("A question must have between one and ten source references.");
        }

        if (input.Resources.Count > 20 || input.Resources.Any(resource => resource is null))
        {
            throw Invalid("A question may have at most twenty related resources.");
        }

        if (input.Tags.Count > 10 ||
            input.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Trim().Length > 60) ||
            input.Tags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != input.Tags.Count)
        {
            throw Invalid("Question tags must be unique, non-empty, and no longer than 60 characters.");
        }

        foreach (var source in input.Sources)
        {
            ValidateReference(source);
        }

        foreach (var resource in input.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.Title) || string.IsNullOrWhiteSpace(resource.Url))
            {
                throw Invalid("Each related resource must include a title and URL.");
            }

            ValidateHttpsUrl(resource.Url, "resource URL");
        }

        if (!string.IsNullOrWhiteSpace(input.ModelAnswerContent))
        {
            if (input.ModelAnswerReferences.Count is < 1 or > 10)
            {
                throw Invalid("A model answer must include between one and ten references.");
            }

            foreach (var reference in input.ModelAnswerReferences)
            {
                ValidateReference(reference);
            }

        }
    }

    private static void ValidateReference(WrittenSourceInput source)
    {
        if (source is null ||
            string.IsNullOrWhiteSpace(source.Title) ||
            string.IsNullOrWhiteSpace(source.Publisher) ||
            string.IsNullOrWhiteSpace(source.Url))
        {
            throw Invalid("Each source reference must include a title, publisher, and URL.");
        }

        ValidateHttpsUrl(source.Url, "source URL");
        _ = ParseEnum<WrittenReferenceStatus>(source.VerificationStatus, "reference status");
    }

    private void ApplyQuestionInput(WrittenQuestion question, WrittenQuestionInput input, Guid actorId)
    {
        question.Prompt = input.Prompt.Trim();
        question.Type = ParseEnum<WrittenQuestionType>(input.Type, "question type");
        question.BcsExamYear = input.BcsExamYear;
        question.ExamNumber = input.ExamNumber;
        question.ExamReference = Normalize(input.ExamReference);
        question.Marks = input.Marks;
        question.AnswerGuidance = Normalize(input.AnswerGuidance);

        question.Sources.Clear();
        foreach (var source in input.Sources)
        {
            question.Sources.Add(new WrittenQuestionSourceReference
            {
                Id = Guid.NewGuid(),
                Title = source.Title.Trim(),
                Publisher = source.Publisher.Trim(),
                Url = source.Url.Trim(),
                Citation = Normalize(source.Citation),
                PublishedOn = source.PublishedOn,
                VerificationStatus = ParseEnum<WrittenReferenceStatus>(source.VerificationStatus, "reference status"),
            });
        }

        question.Resources.Clear();
        foreach (var resource in input.Resources)
        {
            question.Resources.Add(new WrittenQuestionResource
            {
                Id = Guid.NewGuid(),
                Title = resource.Title.Trim(),
                Url = resource.Url.Trim(),
            });
        }

        question.Tags.Clear();
        foreach (var tag in input.Tags.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            question.Tags.Add(new WrittenQuestionTag { QuestionId = question.Id, Tag = tag });
        }

        if (!string.IsNullOrWhiteSpace(input.ModelAnswerContent))
        {
            if (question.ModelAnswer is null)
            {
                question.ModelAnswer = new WrittenModelAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
            }

            question.ModelAnswer.Content = input.ModelAnswerContent.Trim();
            question.ModelAnswer.Status = WrittenModelAnswerStatus.Draft;
            question.ModelAnswer.UpdatedById = actorId;
            question.ModelAnswer.UpdatedAt = DateTimeOffset.UtcNow;
            question.ModelAnswer.References.Clear();
            foreach (var reference in input.ModelAnswerReferences)
            {
                question.ModelAnswer.References.Add(new WrittenModelAnswerReference
                {
                    Id = Guid.NewGuid(),
                    Title = reference.Title.Trim(),
                    Publisher = reference.Publisher.Trim(),
                    Url = reference.Url.Trim(),
                    Citation = Normalize(reference.Citation),
                    VerificationStatus = ParseEnum<WrittenReferenceStatus>(reference.VerificationStatus, "reference status"),
                });
            }
        }
    }

    private async Task<WrittenAdminQuestionDto> GetAdminQuestionDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.WrittenQuestions.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == id, cancellationToken);
        return ToAdminQuestionDto(question);
    }

    private static WrittenQuestionDto ToQuestionDto(WrittenQuestion question, bool isBookmarked) =>
        new(
            question.Id,
            question.SyllabusItemId,
            question.SyllabusItem.SyllabusSubject.SubjectId,
            question.SyllabusItem.SyllabusSubject.Subject.Name,
            question.SyllabusItem.SyllabusSubject.SyllabusVersionId,
            question.SyllabusItem.SyllabusSubject.SyllabusVersion.VersionLabel,
            question.SyllabusItem.Title,
            question.SyllabusItem.Parent?.Title,
            question.SyllabusItem.PaperId,
            question.SyllabusItem.Paper?.Name,
            question.Prompt,
            question.Type.ToString(),
            question.BcsExamYear,
            question.ExamNumber,
            question.ExamReference,
            question.Marks,
            question.AnswerGuidance,
            question.Status.ToString(),
            isBookmarked,
            question.ModelAnswer?.Status == WrittenModelAnswerStatus.Published,
            question.Sources.OrderBy(source => source.Title).Select(ToSourceDto).ToArray(),
            question.Resources.OrderBy(resource => resource.Title)
                .Select(resource => new WrittenQuestionResourceDto(resource.Id, resource.Title, resource.Url)).ToArray(),
            question.Tags.Select(tag => tag.Tag).OrderBy(tag => tag).ToArray());

    private static WrittenAdminQuestionDto ToAdminQuestionDto(WrittenQuestion question) =>
        new(
            ToQuestionDto(question, false),
            question.ModelAnswer?.Content,
            question.ModelAnswer?.Status.ToString(),
            question.ModelAnswer?.References.OrderBy(reference => reference.Title).Select(ToSourceDto).ToArray() ?? []);

    private static WrittenQuestionSourceDto ToSourceDto(WrittenQuestionSourceReference source) =>
        new(source.Id, source.Title, source.Publisher, source.Url, source.Citation, source.PublishedOn, source.VerificationStatus.ToString());

    private static WrittenQuestionSourceDto ToSourceDto(WrittenModelAnswerReference source) =>
        new(source.Id, source.Title, source.Publisher, source.Url, source.Citation, null, source.VerificationStatus.ToString());

    private void AddAudit(Guid actorId, string action, string entityType, Guid entityId)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier,
        });
    }

    private static T ParseEnum<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw Invalid($"The requested {field} is not supported.");

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateHttpsUrl(string value, string field)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw Invalid($"The {field} must be an absolute HTTPS URL.");
        }
    }

    private static WrittenException Invalid(string detail) =>
        new(StatusCodes.Status400BadRequest, "Invalid Written request", detail);
}

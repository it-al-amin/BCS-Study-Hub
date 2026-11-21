using System.Security.Claims;
using System.Text.Json;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Mcq;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Mcq;

internal sealed partial class McqService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : IMcqService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResponse<McqQuestionDto>> GetQuestionsAsync(
        McqQuestionQuery query,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var questions = dbContext.McqQuestions.AsNoTracking()
            .Where(question =>
                question.Status == McqContentStatus.Published &&
                question.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                question.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Preliminary")
            .AsQueryable();

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

        if (query.BcsExamYear is int examYear)
        {
            if (examYear is < 1900 or > 2200)
            {
                throw Invalid("BCS exam year must be between 1900 and 2200.");
            }

            questions = questions.Where(question => question.BcsExamYear == examYear);
        }

        if (!string.IsNullOrWhiteSpace(query.Difficulty))
        {
            var difficulty = ParseEnum<McqDifficulty>(query.Difficulty, "difficulty");
            questions = questions.Where(question => question.Difficulty == difficulty);
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
            if (userId is not Guid authenticatedUserId)
            {
                throw new McqException(StatusCodes.Status401Unauthorized, "Authentication required", "Sign in to filter bookmarked questions.");
            }

            questions = questions.Where(question => dbContext.McqBookmarks.Any(bookmark =>
                bookmark.UserId == authenticatedUserId && bookmark.QuestionId == question.Id));
        }

        questions = query.SortBy.ToLowerInvariant() switch
        {
            "recent" => questions.OrderByDescending(question => question.CreatedAt).ThenBy(question => question.Id),
            "year" => questions.OrderByDescending(question => question.BcsExamYear).ThenBy(question => question.Id),
            "difficulty" => questions.OrderBy(question => question.Difficulty).ThenBy(question => question.Id),
            _ => throw Invalid("SortBy must be one of: recent, year, difficulty."),
        };

        var total = await questions.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await IncludeQuestionDetails(questions)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        HashSet<Guid> bookmarkedIds = userId is Guid currentUserId
            ? await dbContext.McqBookmarks.AsNoTracking()
                .Where(bookmark => bookmark.UserId == currentUserId && rows.Select(row => row.Id).Contains(bookmark.QuestionId))
                .Select(bookmark => bookmark.QuestionId)
                .ToHashSetAsync(cancellationToken)
            : [];
        return new PagedResponse<McqQuestionDto>(
            rows.Select(question => ToQuestionDto(question, bookmarkedIds.Contains(question.Id))).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<McqQuestionDto?> GetQuestionAsync(Guid id, Guid? userId, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.McqQuestions.AsNoTracking()
                .Where(candidate =>
                    candidate.Id == id &&
                    candidate.Status == McqContentStatus.Published &&
                    candidate.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                    candidate.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Preliminary"))
            .SingleOrDefaultAsync(cancellationToken);
        if (question is null)
        {
            return null;
        }

        var bookmarked = userId is Guid currentUserId && await dbContext.McqBookmarks.AnyAsync(
            bookmark => bookmark.UserId == currentUserId && bookmark.QuestionId == id,
            cancellationToken);
        return ToQuestionDto(question, bookmarked);
    }

    public async Task<PagedResponse<McqAdminQuestionDto>> GetAdminQuestionsAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var questions = dbContext.McqQuestions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            questions = questions.Where(question =>
                question.Prompt.Contains(value) ||
                (question.ExamReference != null && question.ExamReference.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var requestedStatus = ParseEnum<McqContentStatus>(status, "status");
            questions = questions.Where(question => question.Status == requestedStatus);
        }

        questions = questions.OrderByDescending(question => question.UpdatedAt).ThenBy(question => question.Id);
        var total = await questions.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await IncludeQuestionDetails(questions)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<McqAdminQuestionDto>(
            rows.Select(ToAdminQuestionDto).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<McqAdminQuestionDto?> GetAdminQuestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.McqQuestions.AsNoTracking())
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return question is null ? null : ToAdminQuestionDto(question);
    }

    public async Task<McqAdminQuestionDto> CreateQuestionAsync(
        McqQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateQuestionInput(input);
        await ValidateQuestionItemAsync(input.SyllabusItemId, cancellationToken);
        var question = new McqQuestion
        {
            Id = Guid.NewGuid(),
            SyllabusItemId = input.SyllabusItemId,
            CreatedById = actorId,
        };
        ApplyQuestionInput(question, input);
        dbContext.McqQuestions.Add(question);
        AddAudit(actorId, "mcq.question.created", nameof(McqQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminQuestionDtoAsync(question.Id, cancellationToken);
    }

    public async Task<McqAdminQuestionDto?> UpdateQuestionAsync(
        Guid id,
        McqQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateQuestionInput(input);
        var question = await IncludeQuestionDetails(dbContext.McqQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (question is null || question.Status == McqContentStatus.Archived)
        {
            return null;
        }

        await ValidateQuestionItemAsync(input.SyllabusItemId, cancellationToken);
        question.SyllabusItemId = input.SyllabusItemId;
        ApplyQuestionInput(question, input);
        AddAudit(actorId, "mcq.question.updated", nameof(McqQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminQuestionDtoAsync(question.Id, cancellationToken);
    }

    public async Task<McqAdminQuestionDto?> SetQuestionStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.McqQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        var newStatus = ParseEnum<McqContentStatus>(status, "status");
        if (newStatus == McqContentStatus.Published)
        {
            await ValidateQuestionCanPublishAsync(question, cancellationToken);
        }
        else if (newStatus == McqContentStatus.Draft && question.Status != McqContentStatus.Draft)
        {
            throw Invalid("Published or archived questions cannot return to draft.");
        }

        question.Status = newStatus;
        AddAudit(actorId, $"mcq.question.{status.ToLowerInvariant()}", nameof(McqQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminQuestionDto(question);
    }

    public async Task<PagedResponse<McqExamSetDto>> GetExamSetsAsync(
        bool includeUnpublished,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var sets = IncludeExamSetDetails(dbContext.McqExamSets.AsNoTracking());
        if (!includeUnpublished)
        {
            sets = sets.Where(set =>
                set.Status == McqContentStatus.Published &&
                set.SyllabusVersion.Status == SyllabusVersionStatus.Published);
        }

        sets = sets.OrderBy(set => set.Name).ThenBy(set => set.Id);
        var total = await sets.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await sets.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<McqExamSetDto>(
            rows.Select(ToExamSetDto).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<McqExamSetDto?> GetAdminExamSetAsync(Guid id, CancellationToken cancellationToken)
    {
        var set = await IncludeExamSetDetails(dbContext.McqExamSets.AsNoTracking())
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return set is null ? null : ToExamSetDto(set);
    }

    public async Task<McqExamSetDto> CreateExamSetAsync(
        McqExamSetInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        await ValidateExamSetInputAsync(input, cancellationToken);
        var set = new McqExamSet
        {
            Id = Guid.NewGuid(),
            CreatedById = actorId,
        };
        ApplyExamSetInput(set, input);
        dbContext.McqExamSets.Add(set);
        AddAudit(actorId, "mcq.exam-set.created", nameof(McqExamSet), set.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExamSetDtoAsync(set.Id, cancellationToken);
    }

    public async Task<McqExamSetDto?> UpdateExamSetAsync(
        Guid id,
        McqExamSetInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var set = await dbContext.McqExamSets.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (set is null || set.Status == McqContentStatus.Archived)
        {
            return null;
        }

        await ValidateExamSetInputAsync(input, cancellationToken);
        ApplyExamSetInput(set, input);
        AddAudit(actorId, "mcq.exam-set.updated", nameof(McqExamSet), set.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExamSetDtoAsync(set.Id, cancellationToken);
    }

    public async Task<McqExamSetDto?> SetExamSetStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var set = await dbContext.McqExamSets.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (set is null)
        {
            return null;
        }

        var newStatus = ParseEnum<McqContentStatus>(status, "status");
        if (newStatus == McqContentStatus.Published)
        {
            await ValidateExamSetCanPublishAsync(set, cancellationToken);
        }
        else if (newStatus == McqContentStatus.Draft && set.Status != McqContentStatus.Draft)
        {
            throw Invalid("Published or archived exam sets cannot return to draft.");
        }

        set.Status = newStatus;
        AddAudit(actorId, $"mcq.exam-set.{status.ToLowerInvariant()}", nameof(McqExamSet), set.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExamSetDtoAsync(set.Id, cancellationToken);
    }

    private static IQueryable<McqQuestion> IncludeQuestionDetails(IQueryable<McqQuestion> questions) =>
        questions
            .Include(question => question.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject)
                    .ThenInclude(mapping => mapping.Subject)
            .Include(question => question.SyllabusItem)
                .ThenInclude(item => item.Parent)
            .Include(question => question.Options)
            .Include(question => question.Sources)
            .Include(question => question.Tags);

    private static IQueryable<McqExamSet> IncludeExamSetDetails(IQueryable<McqExamSet> sets) =>
        sets
            .Include(set => set.SyllabusVersion)
            .Include(set => set.Subject)
            .Include(set => set.SyllabusItem);

    private async Task<McqAdminQuestionDto> GetAdminQuestionDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.McqQuestions.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == id, cancellationToken);
        return ToAdminQuestionDto(question);
    }

    private async Task<McqExamSetDto> GetExamSetDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var set = await IncludeExamSetDetails(dbContext.McqExamSets.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == id, cancellationToken);
        return ToExamSetDto(set);
    }

    private static McqQuestionDto ToQuestionDto(McqQuestion question, bool isBookmarked) =>
        new(
            question.Id,
            question.SyllabusItemId,
            question.SyllabusItem.SyllabusSubject.SubjectId,
            question.SyllabusItem.SyllabusSubject.Subject.Name,
            question.SyllabusItem.Title,
            question.SyllabusItem.Parent?.Title,
            question.Prompt,
            question.ImageUrl,
            question.Type.ToString(),
            question.Difficulty.ToString(),
            question.Status.ToString(),
            question.BcsExamYear,
            question.ExamNumber,
            question.ExamReference,
            isBookmarked,
            question.Options.OrderBy(option => option.SortOrder)
                .Select(option => new McqOptionDto(option.Id, option.Text, option.SortOrder))
                .ToArray(),
            question.Sources.OrderBy(source => source.Title)
                .Select(ToSourceDto)
                .ToArray(),
            question.Tags.Select(tag => tag.Tag).OrderBy(tag => tag).ToArray());

    private static McqAdminQuestionDto ToAdminQuestionDto(McqQuestion question) =>
        new(
            question.Id,
            question.SyllabusItemId,
            question.SyllabusItem.SyllabusSubject.SyllabusVersionId,
            question.Prompt,
            question.Explanation,
            question.ImageUrl,
            question.Type.ToString(),
            question.Difficulty.ToString(),
            question.Status.ToString(),
            question.BcsExamYear,
            question.ExamNumber,
            question.ExamReference,
            question.Options.OrderBy(option => option.SortOrder)
                .Select(option => new McqOptionDto(option.Id, option.Text, option.SortOrder, option.IsCorrect))
                .ToArray(),
            question.Sources.OrderBy(source => source.Title)
                .Select(ToSourceDto)
                .ToArray(),
            question.Tags.Select(tag => tag.Tag).OrderBy(tag => tag).ToArray());

    private static McqQuestionSourceDto ToSourceDto(McqQuestionSourceReference source) =>
        new(source.Id, source.Title, source.Publisher, source.Url, source.Citation, source.PublishedOn, source.VerificationStatus.ToString());

    private static McqExamSetDto ToExamSetDto(McqExamSet set) =>
        new(
            set.Id,
            set.SyllabusVersionId,
            set.SyllabusVersion.VersionLabel,
            set.SubjectId,
            set.Subject?.Name,
            set.SyllabusItemId,
            set.SyllabusItem?.Title,
            set.Name,
            set.Instructions,
            set.Mode.ToString(),
            set.Status.ToString(),
            set.QuestionCount,
            set.DurationSeconds,
            set.BcsExamYear,
            set.Difficulty?.ToString(),
            set.MarksCorrect,
            set.MarksIncorrect,
            set.MarksUnanswered,
            set.ShowFeedbackImmediately);

    private void ApplyQuestionInput(McqQuestion question, McqQuestionInput input)
    {
        question.Prompt = input.Prompt.Trim();
        question.Explanation = Normalize(input.Explanation);
        question.ImageUrl = Normalize(input.ImageUrl);
        question.Type = ParseEnum<McqQuestionType>(input.Type, "type");
        question.Difficulty = ParseEnum<McqDifficulty>(input.Difficulty, "difficulty");
        question.BcsExamYear = input.BcsExamYear;
        question.ExamNumber = input.ExamNumber;
        question.ExamReference = Normalize(input.ExamReference);

        question.Options.Clear();
        for (var index = 0; index < input.Options.Count; index++)
        {
            question.Options.Add(new McqQuestionOption
            {
                Id = Guid.NewGuid(),
                Text = input.Options[index].Text.Trim(),
                SortOrder = index + 1,
                IsCorrect = input.Options[index].IsCorrect,
            });
        }

        question.Sources.Clear();
        foreach (var sourceInput in input.Sources)
        {
            question.Sources.Add(new McqQuestionSourceReference
            {
                Id = Guid.NewGuid(),
                Title = sourceInput.Title.Trim(),
                Publisher = sourceInput.Publisher.Trim(),
                Url = sourceInput.Url.Trim(),
                Citation = Normalize(sourceInput.Citation),
                PublishedOn = sourceInput.PublishedOn,
                VerificationStatus = ParseEnum<McqSourceVerificationStatus>(
                    sourceInput.VerificationStatus,
                    "source verification status"),
            });
        }

        question.Tags.Clear();
        foreach (var tag in input.Tags.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            question.Tags.Add(new McqQuestionTag { QuestionId = question.Id, Tag = tag });
        }
    }

    private void ApplyExamSetInput(McqExamSet set, McqExamSetInput input)
    {
        set.SyllabusVersionId = input.SyllabusVersionId;
        set.SubjectId = input.SubjectId;
        set.SyllabusItemId = input.SyllabusItemId;
        set.Name = input.Name.Trim();
        set.Instructions = input.Instructions.Trim();
        set.Mode = ParseEnum<McqPracticeMode>(input.Mode, "mode");
        set.QuestionCount = input.QuestionCount;
        set.DurationSeconds = input.DurationSeconds;
        set.BcsExamYear = input.BcsExamYear;
        set.Difficulty = string.IsNullOrWhiteSpace(input.Difficulty)
            ? null
            : ParseEnum<McqDifficulty>(input.Difficulty, "difficulty");
        set.MarksCorrect = input.MarksCorrect;
        set.MarksIncorrect = input.MarksIncorrect;
        set.MarksUnanswered = input.MarksUnanswered;
        set.ShowFeedbackImmediately = input.ShowFeedbackImmediately;
    }

    private async Task ValidateQuestionItemAsync(Guid syllabusItemId, CancellationToken cancellationToken)
    {
        var item = await dbContext.SyllabusItems.AsNoTracking()
            .Where(candidate => candidate.Id == syllabusItemId)
            .Select(candidate => new
            {
                candidate.Kind,
                candidate.VerificationStatus,
                VersionStatus = candidate.SyllabusSubject.SyllabusVersion.Status,
                StageCode = candidate.SyllabusSubject.SyllabusVersion.ExamStage.Code,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null ||
            item.StageCode != "Preliminary" ||
            item.VersionStatus != SyllabusVersionStatus.Published ||
            item.Kind is not (SyllabusItemKind.Topic or SyllabusItemKind.Subtopic))
        {
            throw Invalid("Questions must map to a topic or subtopic in a published Preliminary syllabus version.");
        }
    }

    private async Task ValidateQuestionCanPublishAsync(McqQuestion question, CancellationToken cancellationToken)
    {
        ValidateQuestionCorrectOptions(question.Type, question.Options.Select(option => option.IsCorrect).ToArray());
        if (question.Sources.Count == 0 ||
            question.Sources.Any(source => source.VerificationStatus != McqSourceVerificationStatus.Verified))
        {
            throw Invalid("Publishing requires at least one source reference, and all linked sources must be verified.");
        }

        var itemIsVerified = await dbContext.SyllabusItems.AnyAsync(
            item => item.Id == question.SyllabusItemId &&
                    item.VerificationStatus == SyllabusVerificationStatus.Verified &&
                    item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published &&
                    item.SyllabusSubject.SyllabusVersion.ExamStage.Code == "Preliminary",
            cancellationToken);
        if (!itemIsVerified)
        {
            throw Invalid("Publishing requires a verified topic or subtopic in a published Preliminary syllabus version.");
        }
    }

    private static void ValidateQuestionInput(McqQuestionInput input)
    {
        if (input is null ||
            string.IsNullOrWhiteSpace(input.Prompt) ||
            input.Options is null ||
            input.Sources is null ||
            input.Tags is null)
        {
            throw Invalid("Question prompt, options, source references, and tags are required.");
        }

        var type = ParseEnum<McqQuestionType>(input.Type, "type");
        _ = ParseEnum<McqDifficulty>(input.Difficulty, "difficulty");
        if (input.Options.Count is < 2 or > 8)
        {
            throw Invalid("A question must contain between two and eight options.");
        }

        if (input.Options.Any(option => option is null || string.IsNullOrWhiteSpace(option.Text)) ||
            input.Options.Select(option => option.Text.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != input.Options.Count)
        {
            throw Invalid("Question options must be non-empty and unique.");
        }

        ValidateQuestionCorrectOptions(type, input.Options.Select(option => option.IsCorrect).ToArray());
        if (input.Sources.Count is < 1 or > 10)
        {
            throw Invalid("A question must contain between one and ten source references.");
        }

        if (input.Tags.Count > 10 ||
            input.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Trim().Length > 60) ||
            input.Tags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != input.Tags.Count)
        {
            throw Invalid("Question tags must be unique, non-empty, and no longer than 60 characters.");
        }

        if (!string.IsNullOrWhiteSpace(input.ImageUrl))
        {
            ValidateHttpsUrl(input.ImageUrl, "image URL");
        }

        foreach (var source in input.Sources)
        {
            if (source is null ||
                string.IsNullOrWhiteSpace(source.Title) ||
                string.IsNullOrWhiteSpace(source.Publisher))
            {
                throw Invalid("Each question source must include a title and publisher.");
            }

            ValidateHttpsUrl(source.Url, "source URL");
            _ = ParseEnum<McqSourceVerificationStatus>(source.VerificationStatus, "source verification status");
        }

    }

    private static void ValidateQuestionCorrectOptions(McqQuestionType type, IReadOnlyList<bool> correct)
    {
        var count = correct.Count(value => value);
        if ((type == McqQuestionType.SingleAnswer && count != 1) ||
            (type == McqQuestionType.MultipleAnswer && count < 2))
        {
            throw Invalid(type == McqQuestionType.SingleAnswer
                ? "A single-answer question must have exactly one correct option."
                : "A multiple-answer question must have at least two correct options.");
        }
    }

    private async Task ValidateExamSetInputAsync(McqExamSetInput input, CancellationToken cancellationToken)
    {
        if (input is null ||
            string.IsNullOrWhiteSpace(input.Name) ||
            input.QuestionCount is < 1 or > 200 ||
            input.DurationSeconds is < 1 or > 86400)
        {
            throw Invalid("The practice set has invalid required fields.");
        }

        var mode = ParseEnum<McqPracticeMode>(input.Mode, "mode");
        if (input.MarksCorrect <= 0 ||
            decimal.Round(input.MarksCorrect, 2) != input.MarksCorrect ||
            decimal.Round(input.MarksIncorrect, 2) != input.MarksIncorrect ||
            decimal.Round(input.MarksUnanswered, 2) != input.MarksUnanswered)
        {
            throw Invalid("Scoring values must use at most two decimal places, and MarksCorrect must be greater than zero.");
        }

        if (mode == McqPracticeMode.MockExam && input.DurationSeconds is null)
        {
            throw Invalid("A mock examination must define a time limit.");
        }

        if (mode == McqPracticeMode.Topic && input.SyllabusItemId is null)
        {
            throw Invalid("Topic practice requires a topic or subtopic.");
        }

        if (mode == McqPracticeMode.Subject && input.SubjectId is null)
        {
            throw Invalid("Subject practice requires a subject.");
        }

        if (mode != McqPracticeMode.Topic && input.SyllabusItemId is not null)
        {
            throw Invalid("Only topic practice may specify a topic or subtopic.");
        }

        if (mode != McqPracticeMode.Subject && input.SubjectId is not null)
        {
            throw Invalid("Only subject practice may specify a subject.");
        }

        var version = await dbContext.SyllabusVersions.AsNoTracking()
            .Where(candidate => candidate.Id == input.SyllabusVersionId)
            .Select(candidate => new { candidate.Status, StageCode = candidate.ExamStage.Code })
            .SingleOrDefaultAsync(cancellationToken);
        if (version is null ||
            version.Status != SyllabusVersionStatus.Published ||
            version.StageCode != "Preliminary")
        {
            throw Invalid("Practice sets must use a published Preliminary syllabus version.");
        }

        if (input.SubjectId is Guid subjectId &&
            !await dbContext.SyllabusSubjects.AnyAsync(
                mapping => mapping.SyllabusVersionId == input.SyllabusVersionId && mapping.SubjectId == subjectId,
                cancellationToken))
        {
            throw Invalid("The selected subject is not part of this syllabus version.");
        }

        if (input.SyllabusItemId is Guid itemId &&
            !await dbContext.SyllabusItems.AnyAsync(
                item => item.Id == itemId &&
                        item.SyllabusSubject.SyllabusVersionId == input.SyllabusVersionId &&
                        item.VerificationStatus == SyllabusVerificationStatus.Verified,
                cancellationToken))
        {
            throw Invalid("The selected topic must belong to this version and be source-verified.");
        }
    }

    private async Task ValidateExamSetCanPublishAsync(McqExamSet set, CancellationToken cancellationToken)
    {
        var candidates = CandidateQuestions(set);
        var count = await candidates.LongCountAsync(cancellationToken);
        if (count < set.QuestionCount)
        {
            throw Invalid($"This set requests {set.QuestionCount} questions, but only {count} published matching questions are available.");
        }
    }

    private IQueryable<McqQuestion> CandidateQuestions(McqExamSet set)
    {
        var questions = dbContext.McqQuestions.AsNoTracking()
            .Where(question =>
                question.Status == McqContentStatus.Published &&
                question.SyllabusItem.SyllabusSubject.SyllabusVersionId == set.SyllabusVersionId);

        if (set.SubjectId is Guid subjectId)
        {
            questions = questions.Where(question => question.SyllabusItem.SyllabusSubject.SubjectId == subjectId);
        }

        if (set.SyllabusItemId is Guid itemId)
        {
            questions = questions.Where(question =>
                question.SyllabusItemId == itemId || question.SyllabusItem.ParentId == itemId);
        }

        if (set.BcsExamYear is int year)
        {
            questions = questions.Where(question => question.BcsExamYear == year);
        }

        if (set.Difficulty is McqDifficulty difficulty)
        {
            questions = questions.Where(question => question.Difficulty == difficulty);
        }

        return questions;
    }

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
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw Invalid($"The {field} must be an absolute HTTPS URL.");
        }
    }

    private static McqException Invalid(string detail) =>
        new(StatusCodes.Status400BadRequest, "Invalid MCQ request", detail);

    private static McqException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "MCQ content not found", detail);
}

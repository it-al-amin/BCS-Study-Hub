using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Viva;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Viva;

internal sealed partial class VivaService : IVivaService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;

    public VivaService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<VivaCategoryDto>> GetCategoriesAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var categories = dbContext.VivaCategories.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            categories = categories.Where(category => category.IsActive);
        }

        return await categories
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new VivaCategoryDto(
                category.Id,
                category.Slug,
                category.Name,
                category.Description,
                category.DisplayOrder,
                category.IsActive,
                category.Questions.Count(question => question.Status == VivaQuestionStatus.Published)))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetTopicsAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        var questions = PublishedQuestions();
        if (categoryId is Guid id)
        {
            questions = questions.Where(question => question.CategoryId == id);
        }

        return await questions
            .Where(question => question.Topic != null && question.Topic != "")
            .Select(question => question.Topic!)
            .Distinct()
            .OrderBy(topic => topic)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResponse<VivaQuestionDto>> GetQuestionsAsync(
        VivaQuestionQuery query,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var questions = PublishedQuestions();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length is < 2 or > 100)
            {
                throw Invalid("Search text must contain between two and one hundred characters.");
            }

            questions = questions.Where(question =>
                question.Prompt.Contains(search) ||
                (question.Topic != null && question.Topic.Contains(search)) ||
                question.References.Any(reference =>
                    reference.Title.Contains(search) || reference.Publisher.Contains(search)));
        }

        if (query.CategoryId is Guid categoryId)
        {
            questions = questions.Where(question => question.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Topic))
        {
            var topic = query.Topic.Trim();
            if (topic.Length > 160)
            {
                throw Invalid("Topic filters cannot exceed 160 characters.");
            }

            questions = questions.Where(question => question.Topic == topic);
        }

        if (query.BookmarkedOnly)
        {
            if (userId is not Guid currentUserId)
            {
                throw new VivaException(
                    StatusCodes.Status401Unauthorized,
                    "Authentication required",
                    "Sign in to browse bookmarked Viva questions.");
            }

            questions = questions.Where(question => dbContext.VivaBookmarks.Any(bookmark =>
                bookmark.UserId == currentUserId && bookmark.QuestionId == question.Id));
        }

        questions = query.SortBy.ToLowerInvariant() switch
        {
            "recent" => questions.OrderByDescending(question => question.CreatedAt).ThenBy(question => question.Id),
            "category" => questions.OrderBy(question => question.Category.DisplayOrder)
                .ThenBy(question => question.Category.Name).ThenBy(question => question.Id),
            "topic" => questions.OrderBy(question => question.Topic).ThenBy(question => question.Id),
            _ => throw Invalid("SortBy must be one of: recent, category, topic."),
        };

        var total = await questions.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await IncludeQuestionDetails(questions)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        var bookmarkedIds = userId is Guid authenticatedUserId
            ? await dbContext.VivaBookmarks.AsNoTracking()
                .Where(bookmark => bookmark.UserId == authenticatedUserId &&
                                   rows.Select(row => row.Id).Contains(bookmark.QuestionId))
                .Select(bookmark => bookmark.QuestionId)
                .ToHashSetAsync(cancellationToken)
            : [];
        return new PagedResponse<VivaQuestionDto>(
            rows.Select(question => ToQuestionDto(question, bookmarkedIds.Contains(question.Id))).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<VivaQuestionDto?> GetQuestionAsync(
        Guid questionId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(PublishedQuestions().Where(candidate => candidate.Id == questionId))
            .SingleOrDefaultAsync(cancellationToken);
        if (question is null)
        {
            return null;
        }

        var bookmarked = false;
        if (userId is Guid currentUserId)
        {
            bookmarked = await dbContext.VivaBookmarks.AnyAsync(
                bookmark => bookmark.UserId == currentUserId && bookmark.QuestionId == questionId,
                cancellationToken);
            await RecordQuestionViewAsync(currentUserId, questionId, cancellationToken);
        }

        return ToQuestionDto(question, bookmarked);
    }

    public async Task<PagedResponse<VivaAdminQuestionDto>> GetAdminQuestionsAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var questions = dbContext.VivaQuestions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            questions = questions.Where(question =>
                question.Prompt.Contains(value) ||
                (question.Topic != null && question.Topic.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            questions = questions.Where(question => question.Status == ParseEnum<VivaQuestionStatus>(status, "question status"));
        }

        questions = questions.OrderByDescending(question => question.UpdatedAt).ThenBy(question => question.Id);
        var total = await questions.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await IncludeQuestionDetails(questions)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<VivaAdminQuestionDto>(
            rows.Select(question => ToAdminQuestionDto(question)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<VivaAdminQuestionDto?> GetAdminQuestionAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.VivaQuestions.AsNoTracking())
            .SingleOrDefaultAsync(candidate => candidate.Id == questionId, cancellationToken);
        return question is null ? null : ToAdminQuestionDto(question);
    }

    public Task<IReadOnlyList<VivaCategoryDto>> GetAdminCategoriesAsync(CancellationToken cancellationToken) =>
        GetCategoriesAsync(includeInactive: true, cancellationToken);

    public async Task<VivaCategoryDto> CreateCategoryAsync(
        VivaCategoryInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateCategoryInput(input);
        var slug = NormalizeSlug(input.Slug);
        if (await dbContext.VivaCategories.AnyAsync(category => category.Slug == slug, cancellationToken))
        {
            throw Conflict("A Viva category with this slug already exists.");
        }

        var category = new VivaCategory
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = input.Name.Trim(),
            Description = Normalize(input.Description),
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
        };
        dbContext.VivaCategories.Add(category);
        AddAudit(actorId, "viva.category.created", nameof(VivaCategory), category.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToCategoryDtoAsync(category.Id, cancellationToken);
    }

    public async Task<VivaCategoryDto?> UpdateCategoryAsync(
        Guid id,
        VivaCategoryInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateCategoryInput(input);
        var category = await dbContext.VivaCategories.SingleOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);
        if (category is null)
        {
            return null;
        }

        var slug = NormalizeSlug(input.Slug);
        if (await dbContext.VivaCategories.AnyAsync(
                candidate => candidate.Id != id && candidate.Slug == slug,
                cancellationToken))
        {
            throw Conflict("A Viva category with this slug already exists.");
        }

        category.Slug = slug;
        category.Name = input.Name.Trim();
        category.Description = Normalize(input.Description);
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;
        AddAudit(actorId, "viva.category.updated", nameof(VivaCategory), category.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToCategoryDtoAsync(id, cancellationToken);
    }

    public async Task<VivaAdminQuestionDto> CreateQuestionAsync(
        VivaQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateQuestionInput(input);
        await EnsureActiveCategoryAsync(input.CategoryId, cancellationToken);
        var question = new VivaQuestion
        {
            Id = Guid.NewGuid(),
            CategoryId = input.CategoryId,
            CreatedById = actorId,
        };
        ApplyQuestionInput(question, input);
        dbContext.VivaQuestions.Add(question);
        AddAudit(actorId, "viva.question.created", nameof(VivaQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminQuestionDtoAsync(question.Id, cancellationToken);
    }

    public async Task<VivaAdminQuestionDto?> UpdateQuestionAsync(
        Guid id,
        VivaQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ValidateQuestionInput(input);
        var question = await IncludeQuestionDetails(dbContext.VivaQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (question is null || question.Status == VivaQuestionStatus.Archived)
        {
            return null;
        }

        var category = await dbContext.VivaCategories.SingleOrDefaultAsync(
            candidate => candidate.Id == input.CategoryId && candidate.IsActive,
            cancellationToken);
        if (category is null)
        {
            throw Invalid("Select an active Viva category.");
        }

        question.CategoryId = input.CategoryId;
        question.Category = category;
        ApplyQuestionInput(question, input);
        AddAudit(actorId, "viva.question.updated", nameof(VivaQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminQuestionDtoAsync(question.Id, cancellationToken);
    }

    public async Task<VivaAdminQuestionDto?> SetQuestionStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.VivaQuestions)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        var newStatus = ParseEnum<VivaQuestionStatus>(status, "question status");
        if (question.Status == VivaQuestionStatus.Archived && newStatus != VivaQuestionStatus.Archived)
        {
            throw Invalid("Archived Viva questions cannot be restored.");
        }
        else if (newStatus == VivaQuestionStatus.Published)
        {
            await EnsureActiveCategoryAsync(question.CategoryId, cancellationToken);
        }
        else if (newStatus == VivaQuestionStatus.Draft && question.Status != VivaQuestionStatus.Draft)
        {
            throw Invalid("Published or archived questions cannot return to draft.");
        }

        question.Status = newStatus;
        AddAudit(actorId, $"viva.question.{status.ToLowerInvariant()}", nameof(VivaQuestion), question.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminQuestionDto(question);
    }

    private IQueryable<VivaQuestion> PublishedQuestions() =>
        dbContext.VivaQuestions.AsNoTracking()
            .Where(question =>
                question.Status == VivaQuestionStatus.Published &&
                question.Category.IsActive);

    private static IQueryable<VivaQuestion> IncludeQuestionDetails(IQueryable<VivaQuestion> questions) =>
        questions
            .Include(question => question.Category)
            .Include(question => question.References);

    private async Task RecordQuestionViewAsync(
        Guid userId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var history = await dbContext.VivaQuestionHistory.SingleOrDefaultAsync(
            entry => entry.UserId == userId && entry.QuestionId == questionId,
            cancellationToken);
        if (history is null)
        {
            dbContext.VivaQuestionHistory.Add(new VivaQuestionHistory
            {
                UserId = userId,
                QuestionId = questionId,
                LastViewedAt = DateTimeOffset.UtcNow,
                ViewCount = 1,
            });
        }
        else
        {
            history.LastViewedAt = DateTimeOffset.UtcNow;
            history.ViewCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await dbContext.VivaCategories.AnyAsync(
                category => category.Id == categoryId && category.IsActive,
                cancellationToken))
        {
            throw Invalid("Select an active Viva category.");
        }
    }

    private async Task<VivaCategoryDto> ToCategoryDtoAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.VivaCategories.AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new VivaCategoryDto(
                category.Id,
                category.Slug,
                category.Name,
                category.Description,
                category.DisplayOrder,
                category.IsActive,
                category.Questions.Count(question => question.Status == VivaQuestionStatus.Published)))
            .SingleAsync(cancellationToken);

    private async Task<VivaAdminQuestionDto> GetAdminQuestionDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await IncludeQuestionDetails(dbContext.VivaQuestions.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == id, cancellationToken);
        return ToAdminQuestionDto(question);
    }

    private static void ValidateCategoryInput(VivaCategoryInput input)
    {
        if (input is null ||
            string.IsNullOrWhiteSpace(input.Name) ||
            string.IsNullOrWhiteSpace(input.Slug) ||
            input.DisplayOrder is < 0 or > 10000)
        {
            throw Invalid("Category name, slug, and display order are required.");
        }

        if (input.Slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw Invalid("Category slugs may contain only ASCII letters, numbers, and hyphens.");
        }
    }

    private static void ValidateQuestionInput(VivaQuestionInput input)
    {
        if (input is null ||
            input.CategoryId == Guid.Empty ||
            string.IsNullOrWhiteSpace(input.Prompt) ||
            input.References is null ||
            input.References.Count > 20 ||
            input.References.Any(reference => reference is null))
        {
            throw Invalid("A Viva question requires a category, prompt, and a valid reference list.");
        }

        foreach (var reference in input.References)
        {
            if (string.IsNullOrWhiteSpace(reference.Title) ||
                string.IsNullOrWhiteSpace(reference.Publisher) ||
                string.IsNullOrWhiteSpace(reference.Url))
            {
                throw Invalid("Each reference requires a title, publisher, and URL.");
            }

            _ = ParseEnum<VivaReferenceType>(reference.Type, "reference type");
            _ = ParseEnum<VivaReferenceVerificationStatus>(reference.VerificationStatus, "reference verification status");
            if (!Uri.TryCreate(reference.Url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw Invalid("Viva reference URLs must be absolute HTTPS URLs.");
            }
        }
    }

    private static void ApplyQuestionInput(VivaQuestion question, VivaQuestionInput input)
    {
        question.Topic = Normalize(input.Topic);
        question.Prompt = input.Prompt.Trim();
        question.BcsExamYear = input.BcsExamYear;
        question.ExamNumber = input.ExamNumber;
        question.ExamReference = Normalize(input.ExamReference);
        question.References.Clear();
        foreach (var reference in input.References)
        {
            question.References.Add(new VivaQuestionReference
            {
                Id = Guid.NewGuid(),
                Type = ParseEnum<VivaReferenceType>(reference.Type, "reference type"),
                Title = reference.Title.Trim(),
                Publisher = reference.Publisher.Trim(),
                Url = reference.Url.Trim(),
                Citation = Normalize(reference.Citation),
                VerificationStatus = ParseEnum<VivaReferenceVerificationStatus>(
                    reference.VerificationStatus,
                    "reference verification status"),
            });
        }
    }

    private static VivaAdminQuestionDto ToAdminQuestionDto(VivaQuestion question) =>
        new(ToQuestionDto(question, false), question.Status.ToString());

    private static VivaQuestionDto ToQuestionDto(VivaQuestion question, bool isBookmarked) =>
        new(
            question.Id,
            question.CategoryId,
            question.Category.Name,
            question.Topic,
            question.Prompt,
            question.BcsExamYear,
            question.ExamNumber,
            question.ExamReference,
            isBookmarked,
            question.References.OrderBy(reference => reference.Type).ThenBy(reference => reference.Title)
                .Select(reference => new VivaQuestionReferenceDto(
                    reference.Id,
                    reference.Type.ToString(),
                    reference.Title,
                    reference.Publisher,
                    reference.Url,
                    reference.Citation,
                    reference.VerificationStatus.ToString()))
                .ToArray());

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

    private static string NormalizeSlug(string value) =>
        value.Trim().ToLowerInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static T ParseEnum<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw Invalid($"The requested {field} is not supported.");

    private static VivaException Invalid(string detail) =>
        new(StatusCodes.Status400BadRequest, "Invalid Viva request", detail);

    private static VivaException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Viva conflict", detail);
}

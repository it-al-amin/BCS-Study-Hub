using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Viva;
using BCSStudyHub.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Viva;

internal sealed partial class VivaService
{
    public async Task<bool> AddBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.VivaBookmarks.AnyAsync(
            bookmark => bookmark.UserId == userId && bookmark.QuestionId == questionId,
            cancellationToken);
        if (exists)
        {
            return true;
        }

        if (!await PublishedQuestions().AnyAsync(question => question.Id == questionId, cancellationToken))
        {
            return false;
        }

        dbContext.VivaBookmarks.Add(new VivaBookmark
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
        var bookmark = await dbContext.VivaBookmarks.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.QuestionId == questionId,
            cancellationToken);
        if (bookmark is null)
        {
            return false;
        }

        dbContext.VivaBookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<VivaAnswerNoteDto?> GetAnswerNoteAsync(
        Guid userId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var note = await dbContext.VivaAnswerNotes.AsNoTracking()
            .Include(candidate => candidate.Revisions)
            .SingleOrDefaultAsync(candidate =>
                candidate.UserId == userId && candidate.QuestionId == questionId,
                cancellationToken);
        return note is null ? null : ToAnswerNoteDto(note);
    }

    public async Task<VivaAnswerNoteDto> SaveAnswerNoteAsync(
        Guid userId,
        Guid questionId,
        VivaAnswerNoteInput input,
        CancellationToken cancellationToken)
    {
        if (input is null || input.Content is null || input.Content.Length > 20000)
        {
            throw Invalid("Personal answer notes cannot exceed 20,000 characters.");
        }

        var note = await dbContext.VivaAnswerNotes
            .Include(candidate => candidate.Revisions)
            .SingleOrDefaultAsync(candidate =>
                candidate.UserId == userId && candidate.QuestionId == questionId,
                cancellationToken);
        if (note is null)
        {
            if (!await PublishedQuestions().AnyAsync(question => question.Id == questionId, cancellationToken))
            {
                throw new VivaException(
                    StatusCodes.Status404NotFound,
                    "Viva question not found",
                    "Only published Viva questions can have a new personal answer note.");
            }

            note = new VivaAnswerNote
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuestionId = questionId,
                Content = input.Content,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            note.Revisions.Add(new VivaAnswerNoteRevision
            {
                Id = Guid.NewGuid(),
                RevisionNumber = 1,
                Content = input.Content,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            dbContext.VivaAnswerNotes.Add(note);
        }
        else if (!string.Equals(note.Content, input.Content, StringComparison.Ordinal))
        {
            note.Content = input.Content;
            note.UpdatedAt = DateTimeOffset.UtcNow;
            note.Revisions.Add(new VivaAnswerNoteRevision
            {
                Id = Guid.NewGuid(),
                RevisionNumber = note.Revisions.Count + 1,
                Content = input.Content,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAnswerNoteDto(note);
    }

    public async Task<PagedResponse<VivaQuestionHistoryDto>> GetQuestionHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var history = dbContext.VivaQuestionHistory.AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.LastViewedAt)
            .ThenBy(entry => entry.QuestionId);
        var total = await history.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var entries = await history
            .Include(entry => entry.Question)
                .ThenInclude(question => question.Category)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedResponse<VivaQuestionHistoryDto>(
            entries.Select(entry => new VivaQuestionHistoryDto(
                entry.QuestionId,
                entry.Question.Prompt,
                entry.Question.Category.Name,
                entry.Question.Topic,
                entry.LastViewedAt,
                entry.ViewCount)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<PagedResponse<VivaMockSessionListDto>> GetSessionsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var sessions = dbContext.VivaMockSessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAt)
            .ThenBy(session => session.Id);
        var total = await sessions.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await sessions
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .Select(session => new
            {
                session.Id,
                session.Title,
                session.Status,
                session.StartedAt,
                session.CompletedAt,
                QuestionCount = session.Questions.Count,
                session.CommunicationRating,
                session.SubjectKnowledgeRating,
                session.ClarityRating,
                session.ConfidenceRating,
            })
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(session =>
        {
            var ratings = new[]
            {
                session.CommunicationRating,
                session.SubjectKnowledgeRating,
                session.ClarityRating,
                session.ConfidenceRating,
            }.Where(rating => rating.HasValue).Select(rating => rating!.Value).ToArray();
            return new VivaMockSessionListDto(
                session.Id,
                session.Title,
                session.Status.ToString(),
                session.QuestionCount,
                session.StartedAt,
                session.CompletedAt,
                ratings.Length == 0 ? null : (int)Math.Round(ratings.Average(), MidpointRounding.AwayFromZero));
        }).ToArray();
        return new PagedResponse<VivaMockSessionListDto>(
            items,
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<VivaMockSessionDto> CreateSessionAsync(
        Guid userId,
        VivaMockSessionInput input,
        CancellationToken cancellationToken)
    {
        if (input is null ||
            string.IsNullOrWhiteSpace(input.Title) ||
            input.QuestionIds is null ||
            input.QuestionIds.Count is < 1 or > 50 ||
            input.QuestionIds.Any(id => id == Guid.Empty) ||
            input.QuestionIds.Distinct().Count() != input.QuestionIds.Count)
        {
            throw Invalid("A mock Viva requires a title and one to fifty unique questions.");
        }

        var questions = await IncludeQuestionDetails(PublishedQuestions()
                .Where(question => input.QuestionIds.Contains(question.Id)))
            .ToArrayAsync(cancellationToken);
        if (questions.Length != input.QuestionIds.Count)
        {
            throw Invalid("Every selected mock Viva question must be published and belong to an active category.");
        }

        var byId = questions.ToDictionary(question => question.Id);
        var session = new VivaMockSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = input.Title.Trim(),
            StartedAt = DateTimeOffset.UtcNow,
        };
        for (var index = 0; index < input.QuestionIds.Count; index++)
        {
            var question = byId[input.QuestionIds[index]];
            session.Questions.Add(new VivaMockSessionQuestion
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Sequence = index + 1,
                PromptSnapshot = question.Prompt,
                CategorySnapshot = question.Category.Name,
                TopicSnapshot = question.Topic,
            });
        }

        dbContext.VivaMockSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSessionDto(session);
    }

    public async Task<VivaMockSessionDto?> GetSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.VivaMockSessions.AsNoTracking()
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId && candidate.UserId == userId, cancellationToken);
        return session is null ? null : ToSessionDto(session);
    }

    public async Task<VivaMockSessionDto?> UpdateSessionQuestionAsync(
        Guid userId,
        Guid sessionId,
        Guid sessionQuestionId,
        VivaMockSessionQuestionInput input,
        CancellationToken cancellationToken)
    {
        if (input is null || input.AnswerNotes is null || input.AnswerNotes.Length > 20000)
        {
            throw Invalid("Session answer notes cannot exceed 20,000 characters.");
        }

        var session = await dbContext.VivaMockSessions
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId && candidate.UserId == userId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.Status != VivaSessionStatus.InProgress)
        {
            throw Invalid("Completed mock Viva sessions are read-only.");
        }

        var question = session.Questions.SingleOrDefault(candidate => candidate.Id == sessionQuestionId);
        if (question is null)
        {
            return null;
        }

        question.AnswerNotes = input.AnswerNotes;
        question.NeedsRevision = input.NeedsRevision;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSessionDto(session);
    }

    public async Task<VivaMockSessionDto?> CompleteSessionAsync(
        Guid userId,
        Guid sessionId,
        VivaSessionEvaluationInput input,
        CancellationToken cancellationToken)
    {
        ValidateEvaluation(input);
        var session = await dbContext.VivaMockSessions
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId && candidate.UserId == userId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.Status != VivaSessionStatus.InProgress)
        {
            throw Invalid("This mock Viva session has already been completed.");
        }

        if (session.Questions.Count == 0 || session.Questions.Any(question => string.IsNullOrWhiteSpace(question.AnswerNotes)))
        {
            throw Invalid("Add answer notes for every selected question before completing the mock Viva.");
        }

        session.CommunicationRating = input.CommunicationRating;
        session.SubjectKnowledgeRating = input.SubjectKnowledgeRating;
        session.ClarityRating = input.ClarityRating;
        session.ConfidenceRating = input.ConfidenceRating;
        session.FeedbackNotes = Normalize(input.FeedbackNotes);
        session.ImprovementAreas = Normalize(input.ImprovementAreas);
        session.Status = VivaSessionStatus.Completed;
        session.CompletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSessionDto(session);
    }

    public async Task<IReadOnlyList<VivaWeakTopicDto>> GetWeakTopicsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var revisionItems = await dbContext.VivaMockSessionQuestions.AsNoTracking()
            .Where(question =>
                question.NeedsRevision &&
                question.Session.UserId == userId &&
                question.Session.Status == VivaSessionStatus.Completed)
            .Select(question => new { question.CategorySnapshot, question.TopicSnapshot })
            .ToArrayAsync(cancellationToken);
        return revisionItems
            .GroupBy(item => new { item.CategorySnapshot, item.TopicSnapshot })
            .Select(group => new VivaWeakTopicDto(group.Key.CategorySnapshot, group.Key.TopicSnapshot, group.Count()))
            .OrderByDescending(item => item.RevisionCount)
            .ThenBy(item => item.CategoryName)
            .ThenBy(item => item.Topic)
            .Take(30)
            .ToArray();
    }

    public async Task<VivaProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.VivaProfilePreparations.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        return profile is null ? EmptyProfile() : ToProfileDto(profile);
    }

    public async Task<VivaProfileDto> SaveProfileAsync(
        Guid userId,
        VivaProfileInput input,
        CancellationToken cancellationToken)
    {
        if (input is null)
        {
            throw Invalid("A profile-preparation payload is required.");
        }

        var profile = await dbContext.VivaProfilePreparations.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId,
            cancellationToken);
        if (profile is null)
        {
            profile = new VivaProfilePreparation { UserId = userId };
            dbContext.VivaProfilePreparations.Add(profile);
        }

        profile.SelfIntroduction = NormalizeProfileText(input.SelfIntroduction);
        profile.AcademicBackground = NormalizeProfileText(input.AcademicBackground);
        profile.UniversityAndSubject = NormalizeProfileText(input.UniversityAndSubject);
        profile.HomeDistrict = NormalizeProfileText(input.HomeDistrict);
        profile.ProfessionalExperience = NormalizeProfileText(input.ProfessionalExperience);
        profile.CadrePreferences = NormalizeProfileText(input.CadrePreferences);
        profile.InterestsAndHobbies = NormalizeProfileText(input.InterestsAndHobbies);
        profile.StrengthsAndDevelopmentAreas = NormalizeProfileText(input.StrengthsAndDevelopmentAreas);
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToProfileDto(profile);
    }

    private static void ValidateEvaluation(VivaSessionEvaluationInput input)
    {
        if (input is null ||
            input.CommunicationRating is < 1 or > 5 ||
            input.SubjectKnowledgeRating is < 1 or > 5 ||
            input.ClarityRating is < 1 or > 5 ||
            input.ConfidenceRating is < 1 or > 5 ||
            (input.CommunicationRating is null &&
             input.SubjectKnowledgeRating is null &&
             input.ClarityRating is null &&
             input.ConfidenceRating is null &&
             string.IsNullOrWhiteSpace(input.FeedbackNotes) &&
             string.IsNullOrWhiteSpace(input.ImprovementAreas)))
        {
            throw Invalid("Provide valid self-evaluation ratings or feedback before completing the mock Viva.");
        }
    }

    private static VivaAnswerNoteDto ToAnswerNoteDto(VivaAnswerNote note) =>
        new(
            note.QuestionId,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt,
            note.Revisions.OrderByDescending(revision => revision.RevisionNumber)
                .Select(revision => new VivaAnswerNoteRevisionDto(
                    revision.RevisionNumber,
                    revision.Content,
                    revision.CreatedAt))
                .ToArray());

    private static VivaMockSessionDto ToSessionDto(VivaMockSession session) =>
        new(
            session.Id,
            session.Title,
            session.Status.ToString(),
            session.StartedAt,
            session.CompletedAt,
            session.CommunicationRating,
            session.SubjectKnowledgeRating,
            session.ClarityRating,
            session.ConfidenceRating,
            session.FeedbackNotes,
            session.ImprovementAreas,
            session.Questions.OrderBy(question => question.Sequence)
                .Select(question => new VivaMockSessionQuestionDto(
                    question.Id,
                    question.QuestionId,
                    question.Sequence,
                    question.PromptSnapshot,
                    question.CategorySnapshot,
                    question.TopicSnapshot,
                    question.AnswerNotes,
                    question.NeedsRevision))
                .ToArray());

    private static VivaProfileDto EmptyProfile() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null);

    private static VivaProfileDto ToProfileDto(VivaProfilePreparation profile) =>
        new(
            profile.SelfIntroduction,
            profile.AcademicBackground,
            profile.UniversityAndSubject,
            profile.HomeDistrict,
            profile.ProfessionalExperience,
            profile.CadrePreferences,
            profile.InterestsAndHobbies,
            profile.StrengthsAndDevelopmentAreas,
            profile.UpdatedAt);

    private static string NormalizeProfileText(string? value) => value?.Trim() ?? string.Empty;
}

using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Written;
using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Written;

internal sealed partial class WrittenService
{
    public async Task<PagedResponse<WrittenStudyNoteDto>> GetStudyNotesAsync(
        Guid userId,
        Guid? syllabusItemId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var notes = dbContext.WrittenStudyNotes.AsNoTracking()
            .Where(note => note.UserId == userId);
        if (syllabusItemId is Guid itemId)
        {
            notes = notes.Where(note => note.SyllabusItemId == itemId);
        }

        notes = notes.OrderByDescending(note => note.UpdatedAt).ThenBy(note => note.Id);
        var total = await notes.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await notes
            .Include(note => note.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<WrittenStudyNoteDto>(
            rows.Select(ToStudyNoteDto).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<WrittenStudyNoteDto> CreateStudyNoteAsync(
        Guid userId,
        WrittenStudyNoteInput input,
        CancellationToken cancellationToken)
    {
        ValidateStudyNoteInput(input);
        await ValidateStudyNoteItemAsync(input.SyllabusItemId, cancellationToken);
        var note = new WrittenStudyNote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SyllabusItemId = input.SyllabusItemId,
            Title = input.Title.Trim(),
            Content = input.Content.Trim(),
        };
        dbContext.WrittenStudyNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetStudyNoteDtoAsync(userId, note.Id, cancellationToken);
    }

    public async Task<WrittenStudyNoteDto?> UpdateStudyNoteAsync(
        Guid userId,
        Guid noteId,
        WrittenStudyNoteInput input,
        CancellationToken cancellationToken)
    {
        ValidateStudyNoteInput(input);
        var note = await dbContext.WrittenStudyNotes
            .Include(candidate => candidate.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject)
            .SingleOrDefaultAsync(candidate => candidate.Id == noteId && candidate.UserId == userId, cancellationToken);
        if (note is null)
        {
            return null;
        }

        await ValidateStudyNoteItemAsync(input.SyllabusItemId, cancellationToken);
        note.SyllabusItemId = input.SyllabusItemId;
        note.Title = input.Title.Trim();
        note.Content = input.Content.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetStudyNoteDtoAsync(userId, note.Id, cancellationToken);
    }

    public async Task<bool> DeleteStudyNoteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        var note = await dbContext.WrittenStudyNotes.SingleOrDefaultAsync(
            candidate => candidate.Id == noteId && candidate.UserId == userId,
            cancellationToken);
        if (note is null)
        {
            return false;
        }

        dbContext.WrittenStudyNotes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateStudyNoteItemAsync(Guid syllabusItemId, CancellationToken cancellationToken)
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
            throw Invalid("Written notes must link to a topic or subtopic in a published Written syllabus version.");
        }
    }

    private static void ValidateStudyNoteInput(WrittenStudyNoteInput input)
    {
        if (input is null ||
            input.SyllabusItemId == Guid.Empty ||
            string.IsNullOrWhiteSpace(input.Title) ||
            input.Title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(input.Content) ||
            input.Content.Length > 30000)
        {
            throw Invalid("A Written note requires a topic, title, and content within the allowed length.");
        }
    }

    private async Task<WrittenStudyNoteDto> GetStudyNoteDtoAsync(
        Guid userId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        var note = await dbContext.WrittenStudyNotes.AsNoTracking()
            .Include(candidate => candidate.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject)
            .SingleOrDefaultAsync(candidate => candidate.Id == noteId && candidate.UserId == userId, cancellationToken);
        return note is null
            ? throw new InvalidOperationException("The saved Written note could not be loaded.")
            : ToStudyNoteDto(note);
    }

    private static WrittenStudyNoteDto ToStudyNoteDto(WrittenStudyNote note) =>
        new(
            note.Id,
            note.SyllabusItemId,
            note.SyllabusItem.SyllabusSubject.SyllabusVersionId,
            note.SyllabusItem.Title,
            note.Title,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);
}

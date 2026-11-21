using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.CurrentAffairs;

internal sealed partial class CurrentAffairsService
{
    public async Task<PagedResponse<CurrentAffairsBookmarkDto>> GetBookmarksAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var bookmarks = dbContext.CurrentAffairsBookmarks.AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId && bookmark.Entry.Status == CurrentAffairsStatus.Published)
            .OrderByDescending(bookmark => bookmark.CreatedAt)
            .ThenBy(bookmark => bookmark.EntryId);
        var total = await bookmarks.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await bookmarks.Include(bookmark => bookmark.Entry).Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<CurrentAffairsBookmarkDto>(
            rows.Select(row => new CurrentAffairsBookmarkDto(row.EntryId, row.Entry.Title, row.Entry.Category.ToString(), row.Entry.EventDate, row.CreatedAt)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<bool> AddBookmarkAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        if (!await Published().AnyAsync(entry => entry.Id == entryId, cancellationToken))
        {
            return false;
        }

        if (!await dbContext.CurrentAffairsBookmarks.AnyAsync(bookmark => bookmark.UserId == userId && bookmark.EntryId == entryId, cancellationToken))
        {
            dbContext.CurrentAffairsBookmarks.Add(new CurrentAffairsBookmark { UserId = userId, EntryId = entryId, CreatedAt = DateTimeOffset.UtcNow });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> RemoveBookmarkAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var bookmark = await dbContext.CurrentAffairsBookmarks
            .SingleOrDefaultAsync(item => item.UserId == userId && item.EntryId == entryId, cancellationToken);
        if (bookmark is null)
        {
            return false;
        }

        dbContext.CurrentAffairsBookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResponse<CurrentAffairsNoteDto>> GetNotesAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var notes = dbContext.CurrentAffairsStudyNotes.AsNoTracking()
            .Where(note => note.UserId == userId && note.Entry.Status == CurrentAffairsStatus.Published)
            .OrderByDescending(note => note.UpdatedAt)
            .ThenBy(note => note.Id);
        var total = await notes.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await notes.Include(note => note.Entry).Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<CurrentAffairsNoteDto>(
            rows.Select(row => ToNoteDto(row, row.Entry)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<CurrentAffairsNoteDto?> SaveNoteAsync(Guid userId, Guid entryId, CurrentAffairsNoteInput input, CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var entry = await Published().SingleOrDefaultAsync(candidate => candidate.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        var note = await dbContext.CurrentAffairsStudyNotes
            .SingleOrDefaultAsync(item => item.UserId == userId && item.EntryId == entryId, cancellationToken);
        if (note is null)
        {
            note = new CurrentAffairsStudyNote { Id = Guid.NewGuid(), UserId = userId, EntryId = entryId };
            dbContext.CurrentAffairsStudyNotes.Add(note);
        }

        note.Content = input.Content.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToNoteDto(note, entry);
    }

    public async Task<bool> DeleteNoteAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var note = await dbContext.CurrentAffairsStudyNotes
            .SingleOrDefaultAsync(item => item.UserId == userId && item.EntryId == entryId, cancellationToken);
        if (note is null)
        {
            return false;
        }

        dbContext.CurrentAffairsStudyNotes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResponse<CurrentAffairsRevisionDto>> GetRevisionsAsync(Guid userId, bool dueOnly, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var today = CurrentAffairsRules.Today(timeProvider);
        var revisions = dbContext.CurrentAffairsRevisions.AsNoTracking()
            .Where(revision => revision.UserId == userId && revision.Entry.Status == CurrentAffairsStatus.Published);
        if (dueOnly)
        {
            revisions = revisions.Where(revision => revision.NextReviewOn <= today);
        }

        revisions = revisions.OrderBy(revision => revision.NextReviewOn).ThenBy(revision => revision.EntryId);
        var total = await revisions.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await revisions.Include(revision => revision.Entry).Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<CurrentAffairsRevisionDto>(
            rows.Select(row => ToRevisionDto(row, row.Entry, today)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<CurrentAffairsRevisionDto?> AddRevisionAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await Published().SingleOrDefaultAsync(candidate => candidate.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        var today = CurrentAffairsRules.Today(timeProvider);
        var revision = await dbContext.CurrentAffairsRevisions
            .SingleOrDefaultAsync(item => item.UserId == userId && item.EntryId == entryId, cancellationToken);
        if (revision is null)
        {
            revision = new CurrentAffairsRevision
            {
                UserId = userId,
                EntryId = entryId,
                IntervalDays = 0,
                NextReviewOn = today,
                AddedAt = DateTimeOffset.UtcNow,
            };
            dbContext.CurrentAffairsRevisions.Add(revision);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToRevisionDto(revision, entry, today);
    }

    public async Task<CurrentAffairsRevisionDto?> ReviewAsync(Guid userId, Guid entryId, CurrentAffairsReviewInput input, CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var outcome = Parse<CurrentAffairsRevisionOutcome>(input.Outcome, "review outcome");
        var entry = await Published().SingleOrDefaultAsync(candidate => candidate.Id == entryId, cancellationToken);
        var revision = await dbContext.CurrentAffairsRevisions
            .SingleOrDefaultAsync(item => item.UserId == userId && item.EntryId == entryId, cancellationToken);
        if (entry is null || revision is null)
        {
            return null;
        }

        // Current affairs share the user's configurable revision policy and review history with the planner.
        var today = CurrentAffairsRules.Today(timeProvider);
        var policy = await settingsStore.GetPolicyAsync(userId, cancellationToken);
        var generalOutcome = Enum.Parse<RevisionOutcome>(outcome.ToString());
        var before = revision.Step;
        var (step, interval, _) = policy.Next(revision.Step, generalOutcome);
        var now = timeProvider.GetUtcNow();
        revision.Step = step;
        revision.IntervalDays = interval;
        revision.NextReviewOn = today.AddDays(interval);
        revision.ReviewCount++;
        revision.LastOutcome = outcome;
        revision.LastReviewedAt = now;
        dbContext.RevisionLogs.Add(new RevisionLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceType = RevisionSourceType.CurrentAffairs,
            SourceId = entryId,
            Title = entry.Title.Length <= 300 ? entry.Title : entry.Title[..300],
            Outcome = generalOutcome,
            StepBefore = before,
            StepAfter = step,
            IntervalDays = interval,
            ReviewedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToRevisionDto(revision, entry, today);
    }

    public async Task<bool> RemoveRevisionAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var revision = await dbContext.CurrentAffairsRevisions
            .SingleOrDefaultAsync(item => item.UserId == userId && item.EntryId == entryId, cancellationToken);
        if (revision is null)
        {
            return false;
        }

        dbContext.CurrentAffairsRevisions.Remove(revision);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

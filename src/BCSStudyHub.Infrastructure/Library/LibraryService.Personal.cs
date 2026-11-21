using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Library;

internal sealed partial class LibraryService
{
    private const int MaxCollectionsPerUser = 100;
    private const int MaxItemsPerCollection = 500;
    private const int MaxBookmarksPerResource = 200;
    private const int MaxNotesPerResource = 500;

    public async Task<LibraryDashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var visible = VisibleTo(userId);
        var byVisibility = await visible
            .GroupBy(resource => resource.Visibility)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var byKind = await visible
            .GroupBy(resource => resource.Kind)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var bookmarkCount = await dbContext.LibraryBookmarks.AsNoTracking()
            .CountAsync(bookmark => bookmark.UserId == userId && visible.Any(resource => resource.Id == bookmark.ResourceId), cancellationToken);
        var collectionCount = await dbContext.LibraryCollections.AsNoTracking()
            .CountAsync(collection => collection.OwnerId == userId, cancellationToken);
        var progressCounts = await dbContext.LibraryReadingProgress.AsNoTracking()
            .Where(progress => progress.UserId == userId && visible.Any(resource => resource.Id == progress.ResourceId))
            .GroupBy(progress => progress.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var continueReading = await dbContext.LibraryReadingProgress.AsNoTracking()
            .Include(progress => progress.Resource)
            .Include(progress => progress.CurrentChapter)
            .Where(progress =>
                progress.UserId == userId &&
                (progress.Status == LibraryReadingStatus.Reading || progress.Status == LibraryReadingStatus.OnHold) &&
                visible.Any(resource => resource.Id == progress.ResourceId))
            .OrderByDescending(progress => progress.UpdatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);
        var recentRows = await WithSummaryIncludes(visible
                .OrderByDescending(resource => resource.CreatedAt)
                .ThenBy(resource => resource.Id)
                .Take(6))
            .ToListAsync(cancellationToken);
        var recentBookmarks = await dbContext.LibraryBookmarks.AsNoTracking()
            .Include(bookmark => bookmark.Resource)
            .Include(bookmark => bookmark.Chapter)
            .Where(bookmark => bookmark.UserId == userId && visible.Any(resource => resource.Id == bookmark.ResourceId))
            .OrderByDescending(bookmark => bookmark.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        return new LibraryDashboardDto(
            byVisibility.Sum(group => group.Count),
            byVisibility.Where(group => group.Key == LibraryVisibility.Shared).Sum(group => group.Count),
            byVisibility.Where(group => group.Key == LibraryVisibility.Private).Sum(group => group.Count),
            Enum.GetValues<LibraryResourceKind>()
                .Select(kind => new LibraryKindCountDto(
                    kind.ToString(),
                    byKind.Where(group => group.Key == kind).Sum(group => group.Count)))
                .ToArray(),
            bookmarkCount,
            collectionCount,
            progressCounts.Where(group => group.Key == LibraryReadingStatus.Reading).Sum(group => group.Count),
            progressCounts.Where(group => group.Key == LibraryReadingStatus.Completed).Sum(group => group.Count),
            await GetStorageUsageAsync(userId, cancellationToken),
            continueReading.Select(progress => ToProgressDto(progress, progress.Resource)).ToArray(),
            await ToSummariesAsync(recentRows, userId, cancellationToken),
            recentBookmarks.Select(bookmark => ToBookmarkDto(bookmark, bookmark.Resource)).ToArray());
    }

    public async Task<LibraryStorageUsageDto> GetStorageUsageAsync(Guid userId, CancellationToken cancellationToken) =>
        new(
            await GetPrivateUsageAsync(userId, cancellationToken),
            storageOptions.UserQuotaBytes,
            storageOptions.MaxFileSizeBytes,
            LibraryFileRules.AllowedExtensions);

    public async Task<PagedResponse<LibraryBookmarkDto>> GetBookmarksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var visible = VisibleTo(userId);
        var bookmarks = dbContext.LibraryBookmarks.AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId && visible.Any(resource => resource.Id == bookmark.ResourceId))
            .OrderByDescending(bookmark => bookmark.CreatedAt)
            .ThenBy(bookmark => bookmark.Id);
        var total = await bookmarks.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await bookmarks
            .Include(bookmark => bookmark.Resource)
            .Include(bookmark => bookmark.Chapter)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<LibraryBookmarkDto>(
            rows.Select(bookmark => ToBookmarkDto(bookmark, bookmark.Resource)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<LibraryBookmarkDto> AddBookmarkAsync(
        Guid userId,
        LibraryBookmarkInput input,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var resource = await VisibleTo(userId)
            .Include(candidate => candidate.Book)
            .SingleOrDefaultAsync(candidate => candidate.Id == input.ResourceId, cancellationToken)
            ?? throw NotFound("The resource was not found in your library.");
        var chapter = await ResolveChapterAsync(resource.Id, input.ChapterId, cancellationToken);
        ValidatePage(resource, input.Page);

        var existing = await dbContext.LibraryBookmarks
            .Include(bookmark => bookmark.Chapter)
            .FirstOrDefaultAsync(
                bookmark => bookmark.UserId == userId &&
                            bookmark.ResourceId == resource.Id &&
                            bookmark.ChapterId == input.ChapterId &&
                            bookmark.Page == input.Page,
                cancellationToken);
        if (existing is not null)
        {
            existing.Label = Normalize(input.Label) ?? existing.Label;
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToBookmarkDto(existing, resource);
        }

        var count = await dbContext.LibraryBookmarks
            .CountAsync(bookmark => bookmark.UserId == userId && bookmark.ResourceId == resource.Id, cancellationToken);
        if (count >= MaxBookmarksPerResource)
        {
            throw Invalid($"A resource can have at most {MaxBookmarksPerResource} bookmarks.");
        }

        var created = new LibraryBookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = resource.Id,
            ChapterId = chapter?.Id,
            Page = input.Page,
            Label = Normalize(input.Label),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.LibraryBookmarks.Add(created);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToBookmarkDto(created, resource) with { ChapterTitle = chapter?.Title };
    }

    public async Task<bool> DeleteBookmarkAsync(Guid userId, Guid bookmarkId, CancellationToken cancellationToken)
    {
        var bookmark = await dbContext.LibraryBookmarks
            .SingleOrDefaultAsync(candidate => candidate.Id == bookmarkId && candidate.UserId == userId, cancellationToken);
        if (bookmark is null)
        {
            return false;
        }

        dbContext.LibraryBookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResponse<LibraryProgressDto>> GetProgressListAsync(
        Guid userId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var visible = VisibleTo(userId);
        var progress = dbContext.LibraryReadingProgress.AsNoTracking()
            .Where(item => item.UserId == userId && visible.Any(resource => resource.Id == item.ResourceId));
        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = Parse<LibraryReadingStatus>(status, "reading status");
            progress = progress.Where(item => item.Status == parsed);
        }

        progress = progress.OrderByDescending(item => item.UpdatedAt).ThenBy(item => item.ResourceId);
        var total = await progress.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await progress
            .Include(item => item.Resource)
            .Include(item => item.CurrentChapter)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<LibraryProgressDto>(
            rows.Select(item => ToProgressDto(item, item.Resource)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<LibraryProgressDto?> UpdateProgressAsync(
        Guid userId,
        Guid resourceId,
        LibraryProgressInput input,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var resource = await VisibleTo(userId)
            .Include(candidate => candidate.Book)
            .SingleOrDefaultAsync(candidate => candidate.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        var status = Parse<LibraryReadingStatus>(input.Status, "reading status");
        var chapter = await ResolveChapterAsync(resource.Id, input.CurrentChapterId, cancellationToken);
        var totalPages = input.TotalPages ?? resource.Book?.PageCount;
        if (input.CurrentPage is int current && totalPages is int total && current > total)
        {
            throw Invalid("The current page cannot exceed the total page count.");
        }

        var now = DateTimeOffset.UtcNow;
        var progress = await dbContext.LibraryReadingProgress
            .SingleOrDefaultAsync(item => item.UserId == userId && item.ResourceId == resourceId, cancellationToken);
        if (progress is null)
        {
            progress = new LibraryReadingProgress { UserId = userId, ResourceId = resourceId };
            dbContext.LibraryReadingProgress.Add(progress);
        }

        progress.Status = status;
        progress.CurrentChapterId = chapter?.Id;
        progress.CurrentPage = input.CurrentPage;
        progress.TotalPages = totalPages;
        progress.PercentComplete = (input with { TotalPages = totalPages }).ResolvePercent(status);
        if (status != LibraryReadingStatus.NotStarted)
        {
            progress.StartedAt ??= now;
        }

        progress.CompletedAt = status == LibraryReadingStatus.Completed ? progress.CompletedAt ?? now : null;
        progress.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToProgressDto(progress, resource) with { CurrentChapterTitle = chapter?.Title };
    }

    public async Task<bool> ClearProgressAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken)
    {
        var progress = await dbContext.LibraryReadingProgress
            .SingleOrDefaultAsync(item => item.UserId == userId && item.ResourceId == resourceId, cancellationToken);
        if (progress is null)
        {
            return false;
        }

        dbContext.LibraryReadingProgress.Remove(progress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResponse<LibraryNoteDto>> GetNotesAsync(
        Guid userId,
        Guid? resourceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var visible = VisibleTo(userId);
        var notes = dbContext.LibraryPersonalNotes.AsNoTracking()
            .Where(note => note.UserId == userId && visible.Any(resource => resource.Id == note.ResourceId));
        if (resourceId is Guid id)
        {
            notes = notes.Where(note => note.ResourceId == id);
        }

        notes = notes.OrderByDescending(note => note.UpdatedAt).ThenBy(note => note.Id);
        var total = await notes.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await notes
            .Include(note => note.Resource)
            .Include(note => note.Chapter)
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<LibraryNoteDto>(
            rows.Select(note => ToNoteDto(note, note.Resource)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<LibraryNoteDto?> CreateNoteAsync(
        Guid userId,
        Guid resourceId,
        LibraryNoteInput input,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var resource = await VisibleTo(userId)
            .Include(candidate => candidate.Book)
            .SingleOrDefaultAsync(candidate => candidate.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        var chapter = await ResolveChapterAsync(resource.Id, input.ChapterId, cancellationToken);
        ValidatePage(resource, input.Page);
        var count = await dbContext.LibraryPersonalNotes
            .CountAsync(note => note.UserId == userId && note.ResourceId == resourceId, cancellationToken);
        if (count >= MaxNotesPerResource)
        {
            throw Invalid($"A resource can have at most {MaxNotesPerResource} personal notes.");
        }

        var note = new LibraryPersonalNote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = resourceId,
            ChapterId = chapter?.Id,
            Page = input.Page,
            Content = input.Content.Trim(),
        };
        dbContext.LibraryPersonalNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToNoteDto(note, resource) with { ChapterTitle = chapter?.Title };
    }

    public async Task<LibraryNoteDto?> UpdateNoteAsync(
        Guid userId,
        Guid noteId,
        LibraryNoteInput input,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var visible = VisibleTo(userId);
        var note = await dbContext.LibraryPersonalNotes
            .Include(candidate => candidate.Resource).ThenInclude(resource => resource.Book)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == noteId &&
                             candidate.UserId == userId &&
                             visible.Any(resource => resource.Id == candidate.ResourceId),
                cancellationToken);
        if (note is null)
        {
            return null;
        }

        var chapter = await ResolveChapterAsync(note.ResourceId, input.ChapterId, cancellationToken);
        ValidatePage(note.Resource, input.Page);
        note.ChapterId = chapter?.Id;
        note.Page = input.Page;
        note.Content = input.Content.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToNoteDto(note, note.Resource) with { ChapterTitle = chapter?.Title };
    }

    public async Task<bool> DeleteNoteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        var note = await dbContext.LibraryPersonalNotes
            .SingleOrDefaultAsync(candidate => candidate.Id == noteId && candidate.UserId == userId, cancellationToken);
        if (note is null)
        {
            return false;
        }

        dbContext.LibraryPersonalNotes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<LibraryCollectionDto>> GetCollectionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var visible = VisibleTo(userId);
        var rows = await dbContext.LibraryCollections.AsNoTracking()
            .Where(collection => collection.OwnerId == userId)
            .OrderBy(collection => collection.Name)
            .Select(collection => new
            {
                Collection = collection,
                ItemCount = collection.Items.Count(item => visible.Any(resource => resource.Id == item.ResourceId)),
            })
            .ToListAsync(cancellationToken);
        return rows.Select(row => ToCollectionDto(row.Collection, row.ItemCount)).ToArray();
    }

    public async Task<LibraryCollectionDetailDto?> GetCollectionAsync(
        Guid userId,
        Guid collectionId,
        CancellationToken cancellationToken)
    {
        var collection = await dbContext.LibraryCollections.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == collectionId && candidate.OwnerId == userId, cancellationToken);
        if (collection is null)
        {
            return null;
        }

        var order = await dbContext.LibraryCollectionItems.AsNoTracking()
            .Where(item => item.CollectionId == collectionId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.AddedAt)
            .Select(item => item.ResourceId)
            .ToListAsync(cancellationToken);
        var rows = await WithSummaryIncludes(VisibleTo(userId).Where(resource => order.Contains(resource.Id)))
            .ToListAsync(cancellationToken);
        var position = order.Select((id, index) => (id, index)).ToDictionary(pair => pair.id, pair => pair.index);
        var ordered = rows.OrderBy(resource => position[resource.Id]).ToArray();
        return new LibraryCollectionDetailDto(
            ToCollectionDto(collection, ordered.Length),
            await ToSummariesAsync(ordered, userId, cancellationToken));
    }

    public async Task<LibraryCollectionDto> CreateCollectionAsync(
        Guid userId,
        LibraryCollectionInput input,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var name = input.Name.Trim();
        var count = await dbContext.LibraryCollections.CountAsync(collection => collection.OwnerId == userId, cancellationToken);
        if (count >= MaxCollectionsPerUser)
        {
            throw Invalid($"You can create at most {MaxCollectionsPerUser} collections.");
        }

        await EnsureUniqueCollectionNameAsync(userId, name, null, cancellationToken);
        var collection = new LibraryCollection
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = name,
            Description = Normalize(input.Description),
        };
        dbContext.LibraryCollections.Add(collection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCollectionDto(collection, 0);
    }

    public async Task<LibraryCollectionDto?> UpdateCollectionAsync(
        Guid userId,
        Guid collectionId,
        LibraryCollectionInput input,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var collection = await dbContext.LibraryCollections
            .SingleOrDefaultAsync(candidate => candidate.Id == collectionId && candidate.OwnerId == userId, cancellationToken);
        if (collection is null)
        {
            return null;
        }

        var name = input.Name.Trim();
        await EnsureUniqueCollectionNameAsync(userId, name, collectionId, cancellationToken);
        collection.Name = name;
        collection.Description = Normalize(input.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        var visible = VisibleTo(userId);
        var itemCount = await dbContext.LibraryCollectionItems.CountAsync(
            item => item.CollectionId == collectionId && visible.Any(resource => resource.Id == item.ResourceId),
            cancellationToken);
        return ToCollectionDto(collection, itemCount);
    }

    public async Task<bool> DeleteCollectionAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken)
    {
        var collection = await dbContext.LibraryCollections
            .SingleOrDefaultAsync(candidate => candidate.Id == collectionId && candidate.OwnerId == userId, cancellationToken);
        if (collection is null)
        {
            return false;
        }

        dbContext.LibraryCollections.Remove(collection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddCollectionItemAsync(
        Guid userId,
        Guid collectionId,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var collection = await dbContext.LibraryCollections
            .SingleOrDefaultAsync(candidate => candidate.Id == collectionId && candidate.OwnerId == userId, cancellationToken);
        if (collection is null)
        {
            return false;
        }

        if (!await VisibleTo(userId).AnyAsync(resource => resource.Id == resourceId, cancellationToken))
        {
            throw NotFound("The resource was not found in your library.");
        }

        var items = dbContext.LibraryCollectionItems.Where(item => item.CollectionId == collectionId);
        if (await items.AnyAsync(item => item.ResourceId == resourceId, cancellationToken))
        {
            return true;
        }

        var count = await items.CountAsync(cancellationToken);
        if (count >= MaxItemsPerCollection)
        {
            throw Invalid($"A collection can hold at most {MaxItemsPerCollection} resources.");
        }

        var nextOrder = count == 0 ? 1 : await items.MaxAsync(item => item.SortOrder, cancellationToken) + 1;
        dbContext.LibraryCollectionItems.Add(new LibraryCollectionItem
        {
            CollectionId = collectionId,
            ResourceId = resourceId,
            SortOrder = nextOrder,
            AddedAt = DateTimeOffset.UtcNow,
        });
        collection.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveCollectionItemAsync(
        Guid userId,
        Guid collectionId,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.LibraryCollectionItems
            .Include(candidate => candidate.Collection)
            .SingleOrDefaultAsync(
                candidate => candidate.CollectionId == collectionId &&
                             candidate.ResourceId == resourceId &&
                             candidate.Collection.OwnerId == userId,
                cancellationToken);
        if (item is null)
        {
            return false;
        }

        dbContext.LibraryCollectionItems.Remove(item);
        item.Collection.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<LibraryBookChapter?> ResolveChapterAsync(
        Guid resourceId,
        Guid? chapterId,
        CancellationToken cancellationToken)
    {
        if (chapterId is not Guid id)
        {
            return null;
        }

        return await dbContext.LibraryBookChapters.AsNoTracking()
            .SingleOrDefaultAsync(chapter => chapter.Id == id && chapter.ResourceId == resourceId, cancellationToken)
            ?? throw Invalid("The chapter does not belong to this resource.");
    }

    private static void ValidatePage(LibraryResource resource, int? page)
    {
        if (page is int value && resource.Book?.PageCount is int pageCount && value > pageCount)
        {
            throw Invalid($"The page cannot exceed the book's {pageCount} pages.");
        }
    }

    private async Task EnsureUniqueCollectionNameAsync(
        Guid userId,
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.LibraryCollections.AnyAsync(
            collection => collection.OwnerId == userId && collection.Name == name && collection.Id != exceptId,
            cancellationToken);
        if (exists)
        {
            throw Conflict("You already have a collection with this name.");
        }
    }

    private static LibraryCollectionDto ToCollectionDto(LibraryCollection collection, int itemCount) =>
        new(collection.Id, collection.Name, collection.Description, itemCount, collection.CreatedAt, collection.UpdatedAt);
}

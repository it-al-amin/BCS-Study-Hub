using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Configuration;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Infrastructure.Library;

/// <summary>
/// Digital library. Access rules:
/// <list type="bullet">
/// <item>Private resources are visible to and managed by their owner only; other users receive 404.</item>
/// <item>Shared resources are managed by holders of <c>library.manage</c> and are visible to learners once published.</item>
/// <item>Bookmarks, reading progress, notes and collections are owner-scoped and may only reference visible resources.</item>
/// </list>
/// </summary>
internal sealed partial class LibraryService(
    ApplicationDbContext dbContext,
    IFileStorage storage,
    LibraryStorageOptions storageOptions,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LibraryService> logger) : ILibraryService
{
    private const int MaxChaptersPerBook = 1000;

    public async Task<PagedResponse<LibraryResourceSummaryDto>> GetResourcesAsync(
        LibraryResourceQuery query,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var resources = VisibleTo(userId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length is < 2 or > 100)
            {
                throw Invalid("Search text must contain between two and one hundred characters.");
            }

            var isbn = LibraryRules.NormalizeIsbn(search);
            resources = resources.Where(resource =>
                resource.Title.Contains(search) ||
                (resource.Authors != null && resource.Authors.Contains(search)) ||
                (resource.Description != null && resource.Description.Contains(search)) ||
                (resource.SourceName != null && resource.SourceName.Contains(search)) ||
                (isbn != null && resource.Book != null && resource.Book.Isbn == isbn) ||
                resource.Tags.Any(tag => tag.Tag == search));
        }

        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            var kind = Parse<LibraryResourceKind>(query.Kind, "resource kind");
            resources = resources.Where(resource => resource.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = Parse<LibraryResourceCategory>(query.Category, "resource category");
            resources = resources.Where(resource => resource.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.Format))
        {
            var format = Parse<LibraryFileFormat>(query.Format, "file format");
            resources = resources.Where(resource =>
                resource.Files.Any(file => file.Role == LibraryFileRole.Primary && file.Format == format));
        }

        if (query.SubjectId is Guid subjectId)
        {
            resources = resources.Where(resource =>
                resource.Subjects.Any(subject => subject.SubjectId == subjectId) ||
                resource.Topics.Any(topic => topic.SyllabusItem.SyllabusSubject.SubjectId == subjectId));
        }

        if (query.SyllabusItemId is Guid itemId)
        {
            resources = resources.Where(resource =>
                resource.Topics.Any(topic => topic.SyllabusItemId == itemId || topic.SyllabusItem.ParentId == itemId) ||
                resource.Chapters.Any(chapter => chapter.Topics.Any(topic =>
                    topic.SyllabusItemId == itemId || topic.SyllabusItem.ParentId == itemId)));
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tag = query.Tag.Trim();
            if (tag.Length > LibraryRules.MaxTagLength)
            {
                throw Invalid($"Tags cannot exceed {LibraryRules.MaxTagLength} characters.");
            }

            resources = resources.Where(resource => resource.Tags.Any(item => item.Tag == tag));
        }

        resources = (query.Scope ?? "all").ToLowerInvariant() switch
        {
            "all" => resources,
            "shared" => resources.Where(resource => resource.Visibility == LibraryVisibility.Shared),
            "mine" => resources.Where(resource => resource.Visibility == LibraryVisibility.Private),
            _ => throw Invalid("Scope must be one of: all, shared, mine."),
        };

        if (query.BookmarkedOnly)
        {
            resources = resources.Where(resource => dbContext.LibraryBookmarks.Any(bookmark =>
                bookmark.UserId == userId && bookmark.ResourceId == resource.Id));
        }

        resources = (query.SortBy ?? "recent").ToLowerInvariant() switch
        {
            "recent" => resources.OrderByDescending(resource => resource.UpdatedAt).ThenBy(resource => resource.Id),
            "added" => resources.OrderByDescending(resource => resource.CreatedAt).ThenBy(resource => resource.Id),
            "title" => resources.OrderBy(resource => resource.Title).ThenBy(resource => resource.Id),
            "published" => resources
                .OrderByDescending(resource => resource.PublicationDate)
                .ThenByDescending(resource => resource.Book != null ? resource.Book.PublicationYear : null)
                .ThenBy(resource => resource.Id),
            _ => throw Invalid("SortBy must be one of: recent, added, title, published."),
        };

        return await ToPageAsync(resources, query.PageNumber, query.PageSize, userId, cancellationToken);
    }

    public async Task<LibraryResourceDetailDto?> GetResourceAsync(
        Guid resourceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var resource = await WithDetailIncludes(VisibleTo(userId).Where(candidate => candidate.Id == resourceId))
            .SingleOrDefaultAsync(cancellationToken);
        return resource is null
            ? null
            : await ToDetailAsync(resource, userId, canEdit: IsPersonalOwner(resource, userId), managerView: false, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var visible = VisibleTo(userId);
        return await dbContext.LibraryResourceTags.AsNoTracking()
            .Where(tag => visible.Any(resource => resource.Id == tag.ResourceId))
            .Select(tag => tag.Tag)
            .Distinct()
            .OrderBy(tag => tag)
            .Take(300)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<LibraryResourceSummaryDto>> GetAdminResourcesAsync(
        LibraryAdminResourceQuery query,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var resources = dbContext.LibraryResources.AsNoTracking()
            .Where(resource => resource.Visibility == LibraryVisibility.Shared);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 100)
            {
                throw Invalid("Search text cannot exceed one hundred characters.");
            }

            resources = resources.Where(resource =>
                resource.Title.Contains(search) ||
                (resource.Authors != null && resource.Authors.Contains(search)) ||
                (resource.SourceName != null && resource.SourceName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = Parse<LibraryResourceStatus>(query.Status, "status");
            resources = resources.Where(resource => resource.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            var kind = Parse<LibraryResourceKind>(query.Kind, "resource kind");
            resources = resources.Where(resource => resource.Kind == kind);
        }

        resources = resources.OrderByDescending(resource => resource.UpdatedAt).ThenBy(resource => resource.Id);
        return await ToPageAsync(resources, query.PageNumber, query.PageSize, actorId, cancellationToken);
    }

    public async Task<LibraryResourceDetailDto?> GetManagedResourceAsync(
        Guid resourceId,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        var resource = await WithDetailIncludes(ManagedBy(actor).AsNoTracking().Where(candidate => candidate.Id == resourceId))
            .SingleOrDefaultAsync(cancellationToken);
        return resource is null
            ? null
            : await ToDetailAsync(resource, actor.UserId, canEdit: true, managerView: true, cancellationToken);
    }

    public async Task<LibraryResourceDetailDto> CreateResourceAsync(
        LibraryResourceInput input,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        ValidateResourceInput(input);
        var visibility = actor.Scope == LibraryManagementScope.Shared ? LibraryVisibility.Shared : LibraryVisibility.Private;
        var resource = new LibraryResource
        {
            Id = Guid.NewGuid(),
            OwnerId = actor.UserId,
            Kind = Parse<LibraryResourceKind>(input.Kind, "resource kind"),
            Visibility = visibility,
            Status = visibility == LibraryVisibility.Shared ? LibraryResourceStatus.Draft : LibraryResourceStatus.Published,
        };
        await ApplyResourceInputAsync(resource, input, cancellationToken);
        dbContext.LibraryResources.Add(resource);
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.resource.created", nameof(LibraryResource), resource.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetManagedResourceAsync(resource.Id, actor, cancellationToken))!;
    }

    public async Task<LibraryResourceDetailDto?> UpdateResourceAsync(
        Guid resourceId,
        LibraryResourceInput input,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        ValidateResourceInput(input);
        var resource = await ManagedBy(actor)
            .Include(candidate => candidate.Book)
            .Include(candidate => candidate.Files)
            .Include(candidate => candidate.Subjects)
            .Include(candidate => candidate.Topics)
            .Include(candidate => candidate.Tags)
            .AsSplitQuery()
            .SingleOrDefaultAsync(candidate => candidate.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        if (Parse<LibraryResourceKind>(input.Kind, "resource kind") != resource.Kind)
        {
            throw Invalid("A resource's kind cannot be changed after it is created. Create a new resource instead.");
        }

        await ApplyResourceInputAsync(resource, input, cancellationToken);
        if (resource.Files.Count > 0 &&
            LibraryRules.ValidateFileRights(resource.Visibility, resource.RightsBasis, resource.RightsNote) is { } rightsError)
        {
            throw Invalid(rightsError);
        }

        resource.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.resource.updated", nameof(LibraryResource), resource.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetManagedResourceAsync(resource.Id, actor, cancellationToken);
    }

    public async Task<LibraryResourceDetailDto?> SetStatusAsync(
        Guid resourceId,
        string status,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        if (actor.Scope != LibraryManagementScope.Shared)
        {
            throw new LibraryException(
                StatusCodes.Status403Forbidden,
                "Not permitted",
                "Only shared library resources have a publication workflow.");
        }

        var resource = await ManagedBy(actor)
            .Include(candidate => candidate.Files)
            .SingleOrDefaultAsync(candidate => candidate.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        var newStatus = Parse<LibraryResourceStatus>(status, "status");
        switch (newStatus)
        {
            case LibraryResourceStatus.Published:
                if (resource.Files.Count > 0 &&
                    LibraryRules.ValidateFileRights(resource.Visibility, resource.RightsBasis, resource.RightsNote) is { } rightsError)
                {
                    throw Invalid(rightsError);
                }

                if (resource.Kind == LibraryResourceKind.ExternalLink && !LibraryRules.IsSafeWebUrl(resource.Url))
                {
                    throw Invalid("An external resource needs a valid URL before it can be published.");
                }

                if (resource.Kind == LibraryResourceKind.File &&
                    resource.Files.All(file => file.Role != LibraryFileRole.Primary))
                {
                    throw Invalid("Upload the file before publishing a file resource.");
                }

                resource.PublishedAt ??= DateTimeOffset.UtcNow;
                break;
            case LibraryResourceStatus.Archived:
                break;
            default:
                throw Invalid("A resource can only be published or archived.");
        }

        resource.Status = newStatus;
        AddAudit(actor.UserId, $"library.shared.resource.{newStatus.ToString().ToLowerInvariant()}", nameof(LibraryResource), resource.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetManagedResourceAsync(resource.Id, actor, cancellationToken);
    }

    public async Task<bool> DeleteResourceAsync(Guid resourceId, LibraryActor actor, CancellationToken cancellationToken)
    {
        var resource = await ManagedBy(actor)
            .Include(candidate => candidate.Files)
            .SingleOrDefaultAsync(candidate => candidate.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return false;
        }

        if (resource.Visibility == LibraryVisibility.Shared && resource.Status == LibraryResourceStatus.Published)
        {
            throw Invalid("Archive a published resource before deleting it.");
        }

        var keys = resource.Files.Select(file => file.StorageKey).ToArray();
        dbContext.LibraryResources.Remove(resource);
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.resource.deleted", nameof(LibraryResource), resource.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DeleteBlobsAsync(keys);
        return true;
    }

    private async Task ApplyResourceInputAsync(
        LibraryResource resource,
        LibraryResourceInput input,
        CancellationToken cancellationToken)
    {
        var rights = Parse<LibraryRightsBasis>(input.RightsBasis, "rights basis");
        var rightsNote = Normalize(input.RightsNote);
        if (resource.Visibility == LibraryVisibility.Shared)
        {
            if (rights != LibraryRightsBasis.NotApplicable && !LibraryRules.AllowsSharedDistribution(rights))
            {
                throw Invalid("A personal copy cannot be shared. Keep it in your private library, or catalogue the book without a file.");
            }

            if (LibraryRules.RequiresRightsNote(rights) && rightsNote is null)
            {
                throw Invalid("Record the licence, permission, or publication terms for this shared resource.");
            }
        }

        resource.Category = Parse<LibraryResourceCategory>(input.Category, "resource category");
        resource.Title = input.Title.Trim();
        resource.Description = Normalize(input.Description);
        resource.Authors = Normalize(input.Authors);
        resource.SourceName = Normalize(input.SourceName);
        resource.SourceType = Parse<LibrarySourceType>(input.SourceType, "source type");
        resource.Website = Normalize(input.Website);
        resource.Url = Normalize(input.Url);
        resource.PublicationDate = input.PublicationDate;
        resource.RightsBasis = rights;
        resource.RightsNote = rightsNote;
        resource.NoteContent = resource.Kind == LibraryResourceKind.Note ? input.NoteContent?.Trim() : Normalize(input.NoteContent);

        if (resource.Kind == LibraryResourceKind.Book)
        {
            resource.Book ??= new LibraryBookDetails { ResourceId = resource.Id };
            var book = input.Book ?? new LibraryBookInput();
            resource.Book.Publisher = Normalize(book.Publisher);
            resource.Book.Edition = Normalize(book.Edition);
            resource.Book.PublicationYear = book.PublicationYear;
            resource.Book.Isbn = LibraryRules.NormalizeIsbn(book.Isbn);
            resource.Book.PageCount = book.PageCount;
            resource.Book.Language = Normalize(book.Language);
        }

        var subjectIds = input.SubjectIds.ToHashSet();
        await ValidateSubjectsAsync(subjectIds, cancellationToken);
        SyncLinks(
            resource.Subjects,
            subjectIds,
            link => link.SubjectId,
            id => new LibraryResourceSubject { ResourceId = resource.Id, SubjectId = id });

        var topicIds = input.SyllabusItemIds.ToHashSet();
        await ValidateTopicsAsync(topicIds, cancellationToken);
        SyncLinks(
            resource.Topics,
            topicIds,
            link => link.SyllabusItemId,
            id => new LibraryResourceTopic { ResourceId = resource.Id, SyllabusItemId = id });

        LibraryRules.TryNormalizeTags(input.Tags, out var tags);
        var tagSet = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in resource.Tags.Where(tag => !tagSet.Contains(tag.Tag)).ToArray())
        {
            resource.Tags.Remove(existing);
        }

        foreach (var tag in tags.Where(tag => resource.Tags.All(existing => !string.Equals(existing.Tag, tag, StringComparison.OrdinalIgnoreCase))))
        {
            resource.Tags.Add(new LibraryResourceTag { ResourceId = resource.Id, Tag = tag });
        }
    }

    private async Task ValidateSubjectsAsync(IReadOnlyCollection<Guid> subjectIds, CancellationToken cancellationToken)
    {
        if (subjectIds.Count == 0)
        {
            return;
        }

        var found = await dbContext.Subjects.AsNoTracking()
            .CountAsync(subject => subjectIds.Contains(subject.Id) && subject.IsActive, cancellationToken);
        if (found != subjectIds.Count)
        {
            throw Invalid("Each subject must be an active subject in the syllabus catalogue.");
        }
    }

    private async Task ValidateTopicsAsync(IReadOnlyCollection<Guid> topicIds, CancellationToken cancellationToken)
    {
        if (topicIds.Count == 0)
        {
            return;
        }

        var found = await dbContext.SyllabusItems.AsNoTracking()
            .CountAsync(item =>
                topicIds.Contains(item.Id) &&
                item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published,
                cancellationToken);
        if (found != topicIds.Count)
        {
            throw Invalid("Each topic must belong to a published syllabus version.");
        }
    }

    private static void SyncLinks<TLink>(
        ICollection<TLink> links,
        IReadOnlySet<Guid> desired,
        Func<TLink, Guid> key,
        Func<Guid, TLink> create)
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

    private IQueryable<LibraryResource> VisibleTo(Guid userId) =>
        dbContext.LibraryResources.AsNoTracking().Where(resource =>
            (resource.Visibility == LibraryVisibility.Private && resource.OwnerId == userId) ||
            (resource.Visibility == LibraryVisibility.Shared && resource.Status == LibraryResourceStatus.Published));

    private IQueryable<LibraryResource> ManagedBy(LibraryActor actor) =>
        actor.Scope == LibraryManagementScope.Shared
            ? dbContext.LibraryResources.Where(resource => resource.Visibility == LibraryVisibility.Shared)
            : dbContext.LibraryResources.Where(resource =>
                resource.Visibility == LibraryVisibility.Private && resource.OwnerId == actor.UserId);

    private static bool IsVisible(LibraryResource resource, Guid userId) =>
        (resource.Visibility == LibraryVisibility.Private && resource.OwnerId == userId) ||
        (resource.Visibility == LibraryVisibility.Shared && resource.Status == LibraryResourceStatus.Published);

    private static bool IsPersonalOwner(LibraryResource resource, Guid userId) =>
        resource.Visibility == LibraryVisibility.Private && resource.OwnerId == userId;

    private static IQueryable<LibraryResource> WithSummaryIncludes(IQueryable<LibraryResource> resources) =>
        resources
            .Include(resource => resource.Book)
            .Include(resource => resource.Files)
            .Include(resource => resource.Chapters)
            .Include(resource => resource.Tags)
            .Include(resource => resource.Subjects).ThenInclude(link => link.Subject)
            .Include(resource => resource.Topics).ThenInclude(link => link.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject).ThenInclude(subject => subject.Subject)
            .Include(resource => resource.Topics).ThenInclude(link => link.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject).ThenInclude(subject => subject.SyllabusVersion)
                .ThenInclude(version => version.ExamStage)
            .AsSplitQuery();

    private static IQueryable<LibraryResource> WithDetailIncludes(IQueryable<LibraryResource> resources) =>
        WithSummaryIncludes(resources)
            .Include(resource => resource.Chapters).ThenInclude(chapter => chapter.Topics)
                .ThenInclude(link => link.SyllabusItem).ThenInclude(item => item.SyllabusSubject)
                .ThenInclude(subject => subject.Subject)
            .Include(resource => resource.Chapters).ThenInclude(chapter => chapter.Topics)
                .ThenInclude(link => link.SyllabusItem).ThenInclude(item => item.SyllabusSubject)
                .ThenInclude(subject => subject.SyllabusVersion).ThenInclude(version => version.ExamStage)
            .Include(resource => resource.Chapters).ThenInclude(chapter => chapter.LinkedResources)
                .ThenInclude(link => link.Resource);

    private async Task<PagedResponse<LibraryResourceSummaryDto>> ToPageAsync(
        IQueryable<LibraryResource> resources,
        int pageNumber,
        int pageSize,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var total = await resources.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await WithSummaryIncludes(resources.Skip(page.Offset).Take(page.EffectivePageSize))
            .ToListAsync(cancellationToken);
        return new PagedResponse<LibraryResourceSummaryDto>(
            await ToSummariesAsync(rows, userId, cancellationToken),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    private async Task<IReadOnlyList<LibraryResourceSummaryDto>> ToSummariesAsync(
        IReadOnlyList<LibraryResource> rows,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(row => row.Id).ToArray();
        var bookmarked = await dbContext.LibraryBookmarks.AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId && ids.Contains(bookmark.ResourceId))
            .Select(bookmark => bookmark.ResourceId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var progress = await dbContext.LibraryReadingProgress.AsNoTracking()
            .Include(item => item.CurrentChapter)
            .Where(item => item.UserId == userId && ids.Contains(item.ResourceId))
            .ToListAsync(cancellationToken);
        var bookmarkSet = bookmarked.ToHashSet();
        var progressMap = progress.ToDictionary(item => item.ResourceId);
        return rows.Select(row => ToSummary(
                row,
                userId,
                bookmarkSet.Contains(row.Id),
                progressMap.TryGetValue(row.Id, out var item) ? ToProgressDto(item, row) : null))
            .ToArray();
    }

    private async Task<LibraryResourceDetailDto> ToDetailAsync(
        LibraryResource resource,
        Guid userId,
        bool canEdit,
        bool managerView,
        CancellationToken cancellationToken)
    {
        var summary = (await ToSummariesAsync([resource], userId, cancellationToken))[0];
        var bookmarks = await dbContext.LibraryBookmarks.AsNoTracking()
            .Include(bookmark => bookmark.Chapter)
            .Where(bookmark => bookmark.UserId == userId && bookmark.ResourceId == resource.Id)
            .OrderBy(bookmark => bookmark.Page)
            .ThenByDescending(bookmark => bookmark.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var notes = await dbContext.LibraryPersonalNotes.AsNoTracking()
            .Include(note => note.Chapter)
            .Where(note => note.UserId == userId && note.ResourceId == resource.Id)
            .OrderByDescending(note => note.UpdatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var visibleBooks = VisibleTo(userId);
        var usedInRows = await dbContext.LibraryChapterResources.AsNoTracking()
            .Where(link => link.ResourceId == resource.Id &&
                           visibleBooks.Any(book => book.Id == link.Chapter.ResourceId))
            .OrderBy(link => link.Chapter.Resource.Title)
            .ThenBy(link => link.Chapter.ChapterNumber)
            .Select(link => new
            {
                link.Chapter.ResourceId,
                BookTitle = link.Chapter.Resource.Title,
                link.Chapter.ChapterNumber,
                ChapterTitle = link.Chapter.Title,
                link.Chapter.Resource.Kind,
            })
            .Take(50)
            .ToListAsync(cancellationToken);
        var usedIn = usedInRows
            .Select(row => new LibraryResourceRefDto(
                row.ResourceId,
                $"{row.BookTitle} - Chapter {row.ChapterNumber}: {row.ChapterTitle}",
                row.Kind.ToString()))
            .ToArray();

        return new LibraryResourceDetailDto(
            summary,
            resource.Website,
            resource.RightsBasis.ToString(),
            resource.RightsNote,
            resource.NoteContent,
            resource.PublishedAt,
            canEdit,
            resource.Files.OrderBy(file => file.Role).Select(ToFileDto).ToArray(),
            resource.Chapters.OrderBy(chapter => chapter.ChapterNumber)
                .Select(chapter => ToChapterDto(chapter, linked => managerView || IsVisible(linked, userId)))
                .ToArray(),
            bookmarks.Select(bookmark => ToBookmarkDto(bookmark, resource)).ToArray(),
            notes.Select(note => ToNoteDto(note, resource)).ToArray(),
            usedIn);
    }

    private static LibraryResourceSummaryDto ToSummary(
        LibraryResource resource,
        Guid userId,
        bool isBookmarked,
        LibraryProgressDto? progress)
    {
        var primary = resource.Files.FirstOrDefault(file => file.Role == LibraryFileRole.Primary);
        var cover = resource.Files.FirstOrDefault(file => file.Role == LibraryFileRole.Cover);
        return new LibraryResourceSummaryDto(
            resource.Id,
            resource.Kind.ToString(),
            resource.Category.ToString(),
            resource.Visibility.ToString(),
            resource.Status.ToString(),
            resource.Title,
            resource.Description,
            resource.Authors,
            resource.SourceName,
            resource.SourceType.ToString(),
            resource.Url,
            resource.PublicationDate,
            resource.Book is null
                ? null
                : new LibraryBookDto(
                    resource.Book.Publisher,
                    resource.Book.Edition,
                    resource.Book.PublicationYear,
                    resource.Book.Isbn,
                    resource.Book.PageCount,
                    resource.Book.Language),
            primary?.Format.ToString(),
            primary?.Id,
            cover?.Id,
            resource.Chapters.Count,
            resource.Subjects
                .Select(link => new LibrarySubjectRefDto(link.SubjectId, link.Subject.Code, link.Subject.Name))
                .OrderBy(subject => subject.Name)
                .ToArray(),
            resource.Topics.Select(link => ToTopicRef(link.SyllabusItem)).OrderBy(topic => topic.Title).ToArray(),
            resource.Tags.Select(tag => tag.Tag).OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
            IsPersonalOwner(resource, userId),
            isBookmarked,
            progress,
            resource.CreatedAt,
            resource.UpdatedAt);
    }

    private static LibraryTopicRefDto ToTopicRef(SyllabusItem item) =>
        new(
            item.Id,
            item.Title,
            item.Kind.ToString(),
            item.SyllabusSubject.SubjectId,
            item.SyllabusSubject.Subject.Name,
            item.SyllabusSubject.SyllabusVersionId,
            item.SyllabusSubject.SyllabusVersion.VersionLabel,
            item.SyllabusSubject.SyllabusVersion.ExamStage.Code);

    private static LibraryChapterDto ToChapterDto(LibraryBookChapter chapter, Func<LibraryResource, bool> canSeeLinked) =>
        new(
            chapter.Id,
            chapter.ChapterNumber,
            chapter.Title,
            chapter.Summary,
            chapter.StartPage,
            chapter.EndPage,
            chapter.Topics.Select(link => ToTopicRef(link.SyllabusItem)).OrderBy(topic => topic.Title).ToArray(),
            chapter.LinkedResources
                .Where(link => canSeeLinked(link.Resource))
                .Select(link => new LibraryResourceRefDto(link.ResourceId, link.Resource.Title, link.Resource.Kind.ToString()))
                .OrderBy(link => link.Title)
                .ToArray());

    private static LibraryFileDto ToFileDto(LibraryFile file) =>
        new(
            file.Id,
            file.Role.ToString(),
            file.Format.ToString(),
            file.OriginalFileName,
            file.ContentType,
            file.SizeBytes,
            file.Sha256,
            file.UploadedAt,
            LibraryFileRules.CanPreviewInline(file.Format));

    private static LibraryProgressDto ToProgressDto(LibraryReadingProgress progress, LibraryResource resource) =>
        new(
            progress.ResourceId,
            resource.Title,
            resource.Kind.ToString(),
            progress.Status.ToString(),
            progress.CurrentChapterId,
            progress.CurrentChapter?.Title,
            progress.CurrentPage,
            progress.TotalPages,
            progress.PercentComplete,
            progress.StartedAt,
            progress.CompletedAt,
            progress.UpdatedAt);

    private static LibraryBookmarkDto ToBookmarkDto(LibraryBookmark bookmark, LibraryResource resource) =>
        new(
            bookmark.Id,
            bookmark.ResourceId,
            resource.Title,
            resource.Kind.ToString(),
            bookmark.ChapterId,
            bookmark.Chapter?.Title,
            bookmark.Page,
            bookmark.Label,
            bookmark.CreatedAt);

    private static LibraryNoteDto ToNoteDto(LibraryPersonalNote note, LibraryResource resource) =>
        new(
            note.Id,
            note.ResourceId,
            resource.Title,
            note.ChapterId,
            note.Chapter?.Title,
            note.Page,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);

    private static void ValidateResourceInput(LibraryResourceInput input)
    {
        ValidateModel(input);
        if (input.Book is not null)
        {
            ValidateModel(input.Book);
        }
    }

    private static void ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            throw Invalid(results[0].ErrorMessage ?? "The request is not valid.");
        }
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

    private async Task DeleteBlobsAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            try
            {
                await storage.DeleteAsync(key, CancellationToken.None);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                logger.LogWarning(exception, "Could not delete library blob {StorageKey}; it is now orphaned.", key);
            }
        }
    }

    private static string ScopeName(LibraryActor actor) =>
        actor.Scope == LibraryManagementScope.Shared ? "shared" : "personal";

    private static T Parse<T>(string? value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _)
            ? parsed
            : throw Invalid($"The requested {field} is not supported.");

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LibraryException Invalid(string message) =>
        new(StatusCodes.Status400BadRequest, "Invalid library request", message);

    private static LibraryException NotFound(string message) =>
        new(StatusCodes.Status404NotFound, "Not found", message);

    private static LibraryException Conflict(string message) =>
        new(StatusCodes.Status409Conflict, "Library conflict", message);
}

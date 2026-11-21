using System.Security.Cryptography;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Infrastructure.Library;

internal sealed partial class LibraryService
{
    public async Task<LibraryFileDto?> UploadFileAsync(
        Guid resourceId,
        string role,
        LibraryUpload upload,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        var fileRole = Parse<LibraryFileRole>(role, "file role");
        var resource = await ManagedBy(actor)
            .Include(candidate => candidate.Files)
            .SingleOrDefaultAsync(candidate => candidate.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        if (resource.Kind is not (LibraryResourceKind.Book or LibraryResourceKind.File))
        {
            throw Invalid("Files can only be attached to book and file resources.");
        }

        if (fileRole == LibraryFileRole.Cover && resource.Kind != LibraryResourceKind.Book)
        {
            throw Invalid("Only books have cover images.");
        }

        if (LibraryRules.ValidateFileRights(resource.Visibility, resource.RightsBasis, resource.RightsNote) is { } rightsError)
        {
            throw Invalid(rightsError);
        }

        var replaced = resource.Files.FirstOrDefault(file => file.Role == fileRole);
        await using var buffered = await EnsureSeekableAsync(upload.Content, cancellationToken);
        var length = buffered.Length;
        if (length <= 0)
        {
            throw Invalid("The uploaded file is empty.");
        }

        if (length > storageOptions.MaxFileSizeBytes)
        {
            throw new LibraryException(
                StatusCodes.Status413PayloadTooLarge,
                "File too large",
                $"Files cannot exceed {FormatBytes(storageOptions.MaxFileSizeBytes)}.");
        }

        if (resource.Visibility == LibraryVisibility.Private)
        {
            var used = await GetPrivateUsageAsync(actor.UserId, cancellationToken) - (replaced?.SizeBytes ?? 0);
            if (used + length > storageOptions.UserQuotaBytes)
            {
                throw Conflict(
                    $"This upload would exceed your {FormatBytes(storageOptions.UserQuotaBytes)} library storage quota. Delete unused files first.");
            }
        }

        var inspection = LibraryFileRules.Inspect(upload.FileName, buffered);
        if (!inspection.IsValid)
        {
            throw new LibraryException(StatusCodes.Status415UnsupportedMediaType, "Unsupported file", inspection.Error!);
        }

        if (fileRole == LibraryFileRole.Cover && inspection.Format != LibraryFileFormat.Image)
        {
            throw new LibraryException(StatusCodes.Status415UnsupportedMediaType, "Unsupported file", "A cover must be a PNG, JPEG, WEBP, or GIF image.");
        }

        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(buffered, cancellationToken));
        buffered.Position = 0;
        var key = storage.CreateKey();
        await storage.SaveAsync(key, buffered, cancellationToken);

        var file = new LibraryFile
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            Role = fileRole,
            Format = inspection.Format,
            OriginalFileName = LibraryFileRules.SanitizeFileName(upload.FileName),
            ContentType = inspection.ContentType,
            SizeBytes = length,
            Sha256 = hash,
            StorageProvider = storage.ProviderName,
            StorageKey = key,
            UploadedById = actor.UserId,
            UploadedAt = DateTimeOffset.UtcNow,
        };
        if (replaced is not null)
        {
            dbContext.LibraryFiles.Remove(replaced);
        }

        dbContext.LibraryFiles.Add(file);
        resource.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.file.uploaded", nameof(LibraryFile), file.Id);
        try
        {
            // One SaveChanges keeps the replacement atomic; EF orders the delete of the old row before the
            // insert of the new one because both share the (ResourceId, Role) unique index.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteBlobsAsync([key]);
            throw;
        }

        if (replaced is not null)
        {
            await DeleteBlobsAsync([replaced.StorageKey]);
        }

        logger.LogInformation(
            "Library file {FileId} ({Format}, {SizeBytes} bytes) stored for resource {ResourceId}.",
            file.Id,
            file.Format,
            file.SizeBytes,
            resource.Id);
        return ToFileDto(file);
    }

    public async Task<bool> DeleteFileAsync(
        Guid resourceId,
        Guid fileId,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        var managed = ManagedBy(actor);
        var file = await dbContext.LibraryFiles
            .Include(candidate => candidate.Resource)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == fileId &&
                             candidate.ResourceId == resourceId &&
                             managed.Any(resource => resource.Id == candidate.ResourceId),
                cancellationToken);
        if (file is null)
        {
            return false;
        }

        if (file.Resource.Visibility == LibraryVisibility.Shared &&
            file.Resource.Status == LibraryResourceStatus.Published &&
            file.Resource.Kind == LibraryResourceKind.File &&
            file.Role == LibraryFileRole.Primary)
        {
            throw Invalid("Archive the resource or upload a replacement instead of removing the file of a published resource.");
        }

        dbContext.LibraryFiles.Remove(file);
        file.Resource.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.file.deleted", nameof(LibraryFile), file.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DeleteBlobsAsync([file.StorageKey]);
        return true;
    }

    public async Task<LibraryFileContent?> OpenFileAsync(
        Guid resourceId,
        Guid fileId,
        Guid userId,
        bool asManager,
        CancellationToken cancellationToken)
    {
        var readable = asManager
            ? dbContext.LibraryResources.AsNoTracking().Where(resource =>
                resource.Visibility == LibraryVisibility.Shared ||
                (resource.Visibility == LibraryVisibility.Private && resource.OwnerId == userId))
            : VisibleTo(userId);
        var file = await dbContext.LibraryFiles.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == fileId &&
                             candidate.ResourceId == resourceId &&
                             readable.Any(resource => resource.Id == candidate.ResourceId),
                cancellationToken);
        if (file is null)
        {
            return null;
        }

        var stream = await storage.OpenReadAsync(file.StorageKey, cancellationToken);
        if (stream is null)
        {
            logger.LogWarning("Library file {FileId} metadata exists but its blob is missing.", file.Id);
            return null;
        }

        return new LibraryFileContent(
            stream,
            file.ContentType,
            file.OriginalFileName,
            file.SizeBytes,
            LibraryFileRules.CanPreviewInline(file.Format));
    }

    public async Task<LibraryChapterDto?> AddChapterAsync(
        Guid resourceId,
        LibraryChapterInput input,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var book = await ManagedBy(actor)
            .Include(resource => resource.Book)
            .Include(resource => resource.Chapters)
            .SingleOrDefaultAsync(resource => resource.Id == resourceId, cancellationToken);
        if (book is null)
        {
            return null;
        }

        if (book.Kind != LibraryResourceKind.Book)
        {
            throw Invalid("Chapters can only be added to books.");
        }

        if (book.Chapters.Count >= MaxChaptersPerBook)
        {
            throw Invalid($"A book can have at most {MaxChaptersPerBook} chapters.");
        }

        if (book.Chapters.Any(chapter => chapter.ChapterNumber == input.ChapterNumber))
        {
            throw Conflict($"Chapter {input.ChapterNumber} already exists in this book.");
        }

        var chapter = new LibraryBookChapter { Id = Guid.NewGuid(), ResourceId = book.Id };
        await ApplyChapterInputAsync(chapter, book, input, actor, cancellationToken);
        dbContext.LibraryBookChapters.Add(chapter);
        book.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.chapter.created", nameof(LibraryBookChapter), chapter.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetChapterDtoAsync(chapter.Id, cancellationToken);
    }

    public async Task<LibraryChapterDto?> UpdateChapterAsync(
        Guid resourceId,
        Guid chapterId,
        LibraryChapterInput input,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        ValidateModel(input);
        var book = await ManagedBy(actor)
            .Include(resource => resource.Book)
            .Include(resource => resource.Chapters).ThenInclude(chapter => chapter.Topics)
            .Include(resource => resource.Chapters).ThenInclude(chapter => chapter.LinkedResources)
            .AsSplitQuery()
            .SingleOrDefaultAsync(resource => resource.Id == resourceId, cancellationToken);
        var chapter = book?.Chapters.SingleOrDefault(candidate => candidate.Id == chapterId);
        if (book is null || chapter is null)
        {
            return null;
        }

        if (book.Chapters.Any(candidate => candidate.Id != chapterId && candidate.ChapterNumber == input.ChapterNumber))
        {
            throw Conflict($"Chapter {input.ChapterNumber} already exists in this book.");
        }

        await ApplyChapterInputAsync(chapter, book, input, actor, cancellationToken);
        book.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.chapter.updated", nameof(LibraryBookChapter), chapter.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetChapterDtoAsync(chapter.Id, cancellationToken);
    }

    public async Task<bool> DeleteChapterAsync(
        Guid resourceId,
        Guid chapterId,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        var managed = ManagedBy(actor);
        var chapter = await dbContext.LibraryBookChapters
            .Include(candidate => candidate.Resource)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == chapterId &&
                             candidate.ResourceId == resourceId &&
                             managed.Any(resource => resource.Id == candidate.ResourceId),
                cancellationToken);
        if (chapter is null)
        {
            return false;
        }

        dbContext.LibraryBookChapters.Remove(chapter);
        chapter.Resource.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actor.UserId, $"library.{ScopeName(actor)}.chapter.deleted", nameof(LibraryBookChapter), chapter.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ApplyChapterInputAsync(
        LibraryBookChapter chapter,
        LibraryResource book,
        LibraryChapterInput input,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        if (book.Book?.PageCount is int pageCount && (input.EndPage > pageCount || input.StartPage > pageCount))
        {
            throw Invalid($"Chapter pages cannot exceed the book's {pageCount} pages.");
        }

        var topicIds = input.SyllabusItemIds.ToHashSet();
        await ValidateTopicsAsync(topicIds, cancellationToken);

        var linkedIds = input.LinkedResourceIds.ToHashSet();
        if (linkedIds.Contains(book.Id))
        {
            throw Invalid("A chapter cannot link to its own book.");
        }

        if (linkedIds.Count > 0)
        {
            // Shared books may only point at shared resources; private books may point at anything the owner can see.
            var candidates = actor.Scope == LibraryManagementScope.Shared
                ? dbContext.LibraryResources.Where(resource => resource.Visibility == LibraryVisibility.Shared)
                : VisibleTo(actor.UserId);
            var found = await candidates.CountAsync(resource => linkedIds.Contains(resource.Id), cancellationToken);
            if (found != linkedIds.Count)
            {
                throw Invalid(actor.Scope == LibraryManagementScope.Shared
                    ? "Chapters of shared books can only link to shared resources."
                    : "Chapters can only link to resources in your library.");
            }
        }

        chapter.ChapterNumber = input.ChapterNumber;
        chapter.Title = input.Title.Trim();
        chapter.Summary = Normalize(input.Summary);
        chapter.StartPage = input.StartPage;
        chapter.EndPage = input.EndPage;
        SyncLinks(
            chapter.Topics,
            topicIds,
            link => link.SyllabusItemId,
            id => new LibraryChapterTopic { ChapterId = chapter.Id, SyllabusItemId = id });
        SyncLinks(
            chapter.LinkedResources,
            linkedIds,
            link => link.ResourceId,
            id => new LibraryChapterResource { ChapterId = chapter.Id, ResourceId = id });
    }

    private async Task<LibraryChapterDto> GetChapterDtoAsync(Guid chapterId, CancellationToken cancellationToken)
    {
        var chapter = await dbContext.LibraryBookChapters.AsNoTracking()
            .Include(candidate => candidate.Topics).ThenInclude(link => link.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject).ThenInclude(subject => subject.Subject)
            .Include(candidate => candidate.Topics).ThenInclude(link => link.SyllabusItem)
                .ThenInclude(item => item.SyllabusSubject).ThenInclude(subject => subject.SyllabusVersion)
                .ThenInclude(version => version.ExamStage)
            .Include(candidate => candidate.LinkedResources).ThenInclude(link => link.Resource)
            .AsSplitQuery()
            .SingleAsync(candidate => candidate.Id == chapterId, cancellationToken);
        return ToChapterDto(chapter, _ => true);
    }

    private async Task<long> GetPrivateUsageAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.LibraryFiles.AsNoTracking()
            .Where(file => file.Resource.Visibility == LibraryVisibility.Private && file.Resource.OwnerId == userId)
            .SumAsync(file => (long?)file.SizeBytes, cancellationToken) ?? 0;

    private static async Task<Stream> EnsureSeekableAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
            return new NonDisposingStream(content);
        }

        var temporary = new FileStream(
            Path.GetTempFileName(),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            // Copy at most one byte beyond the hard ceiling so oversized bodies are detected without buffering them fully.
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > LibraryFileRules.AbsoluteMaxUploadBytes)
                {
                    break;
                }

                await temporary.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            temporary.Position = 0;
            return temporary;
        }
        catch
        {
            await temporary.DisposeAsync();
            throw;
        }
    }

    private static string FormatBytes(long bytes) =>
        bytes >= 1024L * 1024 * 1024
            ? $"{bytes / (1024d * 1024 * 1024):0.#} GB"
            : $"{bytes / (1024d * 1024):0.#} MB";

    /// <summary>Lets the service dispose its wrapper without closing the caller-owned request stream.</summary>
    private sealed class NonDisposingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

using System.Security.Cryptography;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Configuration;
using BCSStudyHub.Infrastructure.Library;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BCSStudyHub.Api.Tests;

/// <summary>
/// Exercises the real library service against an in-memory database and a temporary local file store
/// to verify ownership, publication, copyright, and file-security rules end to end.
/// </summary>
public sealed class LibraryServiceAccessTests : IDisposable
{
    private static readonly byte[] PdfBytes = LibraryFileSecurityTests.SampleFor("a.pdf");
    private readonly string storageRoot = Path.Combine(Path.GetTempPath(), "bcs-library-tests", Guid.NewGuid().ToString("N"));
    private readonly ApplicationDbContext dbContext;
    private readonly LibraryService service;
    private readonly LibraryStorageOptions options = new() { MaxFileSizeBytes = 1024 * 1024, UserQuotaBytes = 2 * 1024 * 1024 };
    private readonly Guid alice = Guid.NewGuid();
    private readonly Guid bob = Guid.NewGuid();
    private readonly Guid manager = Guid.NewGuid();
    private readonly Guid publishedTopicId = Guid.NewGuid();
    private readonly Guid draftTopicId = Guid.NewGuid();
    private readonly Guid subjectId = Guid.NewGuid();

    public LibraryServiceAccessTests()
    {
        dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        SeedSyllabus();
        service = new LibraryService(
            dbContext,
            new LocalFileStorage(storageRoot),
            options,
            new HttpContextAccessor(),
            NullLogger<LibraryService>.Instance);
    }

    private LibraryActor Alice => new(alice, LibraryManagementScope.Personal);
    private LibraryActor Bob => new(bob, LibraryManagementScope.Personal);
    private LibraryActor Manager => new(manager, LibraryManagementScope.Shared);

    [Fact]
    public async Task PrivateResourcesAreInvisibleToOtherUsersAndManagers()
    {
        var created = await service.CreateResourceAsync(Book("Alice's guide", "PersonalCopy"), Alice, default);
        var id = created.Resource.Id;

        Assert.Equal("Private", created.Resource.Visibility);
        Assert.NotNull(await service.GetResourceAsync(id, alice, default));
        Assert.Null(await service.GetResourceAsync(id, bob, default));
        Assert.Empty((await service.GetResourcesAsync(Query(), bob, default)).Items);
        Assert.Null(await service.GetManagedResourceAsync(id, Bob, default));
        Assert.Null(await service.GetManagedResourceAsync(id, Manager, default));
        Assert.Null(await service.UpdateResourceAsync(id, Book("Hijacked", "PersonalCopy"), Bob, default));
        Assert.False(await service.DeleteResourceAsync(id, Bob, default));
        Assert.False(await service.DeleteResourceAsync(id, Manager, default));
    }

    [Fact]
    public async Task PrivateFilesCannotBeDownloadedByOtherUsersEvenAsManager()
    {
        var resource = await service.CreateResourceAsync(Book("Alice's scan", "PersonalCopy"), Alice, default);
        var file = await UploadAsync(resource.Resource.Id, "scan.pdf", PdfBytes, Alice);

        var own = await service.OpenFileAsync(resource.Resource.Id, file!.Id, alice, asManager: false, default);
        Assert.NotNull(own);
        await using (own.Content)
        {
            Assert.Equal("application/pdf", own.ContentType);
            Assert.True(own.CanPreviewInline);
        }

        Assert.Null(await service.OpenFileAsync(resource.Resource.Id, file.Id, bob, asManager: false, default));
        Assert.Null(await service.OpenFileAsync(resource.Resource.Id, file.Id, manager, asManager: true, default));
    }

    [Fact]
    public async Task SharedDraftsAreHiddenUntilPublishedAndHiddenAgainWhenArchived()
    {
        var created = await service.CreateResourceAsync(Link("Bangladesh Economic Review"), Manager, default);
        var id = created.Resource.Id;
        Assert.Equal("Draft", created.Resource.Status);
        Assert.Null(await service.GetResourceAsync(id, alice, default));

        await service.SetStatusAsync(id, "Published", Manager, default);
        Assert.NotNull(await service.GetResourceAsync(id, alice, default));
        Assert.Null(await service.UpdateResourceAsync(id, Link("Edited by learner"), Alice, default));

        await service.SetStatusAsync(id, "Archived", Manager, default);
        Assert.Null(await service.GetResourceAsync(id, alice, default));
    }

    [Fact]
    public async Task LearnersCannotUsePublicationWorkflow()
    {
        var created = await service.CreateResourceAsync(Link("Personal link"), Alice, default);

        var error = await Assert.ThrowsAsync<LibraryException>(() => service.SetStatusAsync(created.Resource.Id, "Published", Alice, default));
        Assert.Equal(StatusCodes.Status403Forbidden, error.StatusCode);
    }

    [Fact]
    public async Task PersonalCopiesCannotBeShared()
    {
        await Assert.ThrowsAsync<LibraryException>(() =>
            service.CreateResourceAsync(Book("Commercial guide", "PersonalCopy"), Manager, default));

        var catalogued = await service.CreateResourceAsync(Book("Commercial guide", "NotApplicable"), Manager, default);
        var error = await Assert.ThrowsAsync<LibraryException>(() =>
            UploadAsync(catalogued.Resource.Id, "guide.pdf", PdfBytes, Manager));
        Assert.Equal(StatusCodes.Status400BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task SharedGovernmentPublicationsRequireARightsNote()
    {
        await Assert.ThrowsAsync<LibraryException>(() =>
            service.CreateResourceAsync(Book("Five-year plan", "GovernmentPublication"), Manager, default));

        var input = Book("Five-year plan", "GovernmentPublication") with { RightsNote = "Published for free public download on plancomm.gov.bd" };
        var resource = await service.CreateResourceAsync(input, Manager, default);
        Assert.NotNull(await UploadAsync(resource.Resource.Id, "plan.pdf", PdfBytes, Manager));
    }

    [Fact]
    public async Task UploadsAreVerifiedHashedAndReplacedCleanly()
    {
        var resource = await service.CreateResourceAsync(Book("Notes", "OwnWork"), Alice, default);
        var id = resource.Resource.Id;

        var spoofed = await Assert.ThrowsAsync<LibraryException>(() =>
            UploadAsync(id, "notes.pdf", [0x4D, 0x5A, 0x90, 0x00], Alice));
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, spoofed.StatusCode);

        var first = await UploadAsync(id, "../../notes.pdf", PdfBytes, Alice);
        Assert.NotNull(first);
        Assert.Equal("notes.pdf", first.OriginalFileName);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(PdfBytes)), first.Sha256);
        var firstKey = (await dbContext.LibraryFiles.AsNoTracking().SingleAsync()).StorageKey;

        var second = await UploadAsync(id, "notes-v2.pdf", [.. PdfBytes, 0x0A], Alice);
        var files = await dbContext.LibraryFiles.AsNoTracking().ToListAsync();
        Assert.Single(files);
        Assert.Equal(second!.Id, files[0].Id);
        Assert.False(File.Exists(Path.Combine(storageRoot, firstKey)));
    }

    [Fact]
    public async Task CoversMustBeImagesAndOnlyBooksHaveThem()
    {
        var book = await service.CreateResourceAsync(Book("Atlas", "OwnWork"), Alice, default);
        await Assert.ThrowsAsync<LibraryException>(() => UploadAsync(book.Resource.Id, "cover.pdf", PdfBytes, Alice, "Cover"));
        Assert.NotNull(await UploadAsync(book.Resource.Id, "cover.png", LibraryFileSecurityTests.SampleFor("c.png"), Alice, "Cover"));

        var link = await service.CreateResourceAsync(Link("A link"), Alice, default);
        await Assert.ThrowsAsync<LibraryException>(() => UploadAsync(link.Resource.Id, "x.pdf", PdfBytes, Alice));
    }

    [Fact]
    public async Task UploadSizeAndQuotaAreEnforced()
    {
        var resource = await service.CreateResourceAsync(Book("Big", "OwnWork"), Alice, default);
        var tooLarge = new byte[options.MaxFileSizeBytes + 1];
        PdfBytes.CopyTo(tooLarge, 0);
        var sizeError = await Assert.ThrowsAsync<LibraryException>(() => UploadAsync(resource.Resource.Id, "big.pdf", tooLarge, Alice));
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, sizeError.StatusCode);

        var almostFull = new byte[options.MaxFileSizeBytes];
        PdfBytes.CopyTo(almostFull, 0);
        await UploadAsync(resource.Resource.Id, "a.pdf", almostFull, Alice);
        var second = await service.CreateResourceAsync(Book("Second", "OwnWork"), Alice, default);
        await UploadAsync(second.Resource.Id, "b.pdf", almostFull, Alice);
        var third = await service.CreateResourceAsync(Book("Third", "OwnWork"), Alice, default);
        var quotaError = await Assert.ThrowsAsync<LibraryException>(() => UploadAsync(third.Resource.Id, "c.pdf", PdfBytes, Alice));
        Assert.Equal(StatusCodes.Status409Conflict, quotaError.StatusCode);
    }

    [Fact]
    public async Task TopicMappingsMustReferencePublishedSyllabusVersions()
    {
        await Assert.ThrowsAsync<LibraryException>(() =>
            service.CreateResourceAsync(Link("Draft-mapped") with { SyllabusItemIds = [draftTopicId] }, Alice, default));

        var mapped = await service.CreateResourceAsync(
            Link("Mapped") with { SyllabusItemIds = [publishedTopicId], SubjectIds = [subjectId] },
            Alice,
            default);
        Assert.Single(mapped.Resource.Topics);
        Assert.Equal("Bangladesh Affairs", mapped.Resource.Subjects.Single().Name);

        var bySubject = await service.GetResourcesAsync(Query() with { SubjectId = subjectId }, alice, default);
        var byTopic = await service.GetResourcesAsync(Query() with { SyllabusItemId = publishedTopicId }, alice, default);
        Assert.Single(bySubject.Items);
        Assert.Single(byTopic.Items);
    }

    [Fact]
    public async Task ChaptersOfSharedBooksCannotLinkToPrivateResources()
    {
        var privateNote = await service.CreateResourceAsync(Note("Private summary"), Alice, default);
        var sharedBook = await service.CreateResourceAsync(Book("Shared book", "NotApplicable"), Manager, default);

        await Assert.ThrowsAsync<LibraryException>(() => service.AddChapterAsync(
            sharedBook.Resource.Id,
            new LibraryChapterInput { ChapterNumber = 1, Title = "Intro", LinkedResourceIds = [privateNote.Resource.Id] },
            Manager,
            default));

        var chapter = await service.AddChapterAsync(
            sharedBook.Resource.Id,
            new LibraryChapterInput { ChapterNumber = 1, Title = "Intro", SyllabusItemIds = [publishedTopicId] },
            Manager,
            default);
        Assert.NotNull(chapter);
        await Assert.ThrowsAsync<LibraryException>(() => service.AddChapterAsync(
            sharedBook.Resource.Id,
            new LibraryChapterInput { ChapterNumber = 1, Title = "Duplicate" },
            Manager,
            default));
    }

    [Fact]
    public async Task PersonalTrackingOnlyReferencesVisibleResources()
    {
        var alicePrivate = await service.CreateResourceAsync(Note("Alice only"), Alice, default);
        var id = alicePrivate.Resource.Id;

        await Assert.ThrowsAsync<LibraryException>(() =>
            service.AddBookmarkAsync(bob, new LibraryBookmarkInput { ResourceId = id }, default));
        Assert.Null(await service.UpdateProgressAsync(bob, id, new LibraryProgressInput { Status = "Reading" }, default));
        Assert.Null(await service.CreateNoteAsync(bob, id, new LibraryNoteInput { Content = "peek" }, default));

        var bobCollection = await service.CreateCollectionAsync(bob, new LibraryCollectionInput { Name = "Bob" }, default);
        await Assert.ThrowsAsync<LibraryException>(() =>
            service.AddCollectionItemAsync(bob, bobCollection.Id, id, default));
        Assert.False(await service.AddCollectionItemAsync(alice, bobCollection.Id, id, default));
    }

    [Fact]
    public async Task BookmarksAreDeduplicatedAndChaptersMustBelongToTheResource()
    {
        var book = await service.CreateResourceAsync(Book("Constitution", "OwnWork"), Alice, default);
        var other = await service.CreateResourceAsync(Book("Economy", "OwnWork"), Alice, default);
        var otherChapter = await service.AddChapterAsync(other.Resource.Id, new LibraryChapterInput { ChapterNumber = 1, Title = "GDP" }, Alice, default);

        await Assert.ThrowsAsync<LibraryException>(() => service.AddBookmarkAsync(
            alice,
            new LibraryBookmarkInput { ResourceId = book.Resource.Id, ChapterId = otherChapter!.Id },
            default));

        var first = await service.AddBookmarkAsync(alice, new LibraryBookmarkInput { ResourceId = book.Resource.Id, Page = 12 }, default);
        var again = await service.AddBookmarkAsync(alice, new LibraryBookmarkInput { ResourceId = book.Resource.Id, Page = 12, Label = "Art. 7" }, default);
        Assert.Equal(first.Id, again.Id);
        Assert.Equal("Art. 7", again.Label);
        Assert.False(await service.DeleteBookmarkAsync(bob, first.Id, default));
    }

    [Fact]
    public async Task ReadingProgressTracksCompletionAndUsesBookPageCount()
    {
        var book = await service.CreateResourceAsync(
            Book("Geography", "OwnWork") with { Book = new LibraryBookInput { PageCount = 200 } },
            Alice,
            default);

        await Assert.ThrowsAsync<LibraryException>(() => service.UpdateProgressAsync(
            alice, book.Resource.Id, new LibraryProgressInput { Status = "Reading", CurrentPage = 250 }, default));

        var reading = await service.UpdateProgressAsync(alice, book.Resource.Id, new LibraryProgressInput { Status = "Reading", CurrentPage = 50 }, default);
        Assert.Equal(25, reading!.PercentComplete);
        Assert.Equal(200, reading.TotalPages);
        Assert.NotNull(reading.StartedAt);

        var done = await service.UpdateProgressAsync(alice, book.Resource.Id, new LibraryProgressInput { Status = "Completed" }, default);
        Assert.Equal(100, done!.PercentComplete);
        Assert.NotNull(done.CompletedAt);
    }

    [Fact]
    public async Task PublishedSharedResourcesMustBeArchivedBeforeDeletionAndDeletionRemovesBlobs()
    {
        var input = Book("Open textbook", "OpenLicense") with { RightsNote = "CC BY 4.0" };
        var resource = await service.CreateResourceAsync(input, Manager, default);
        await UploadAsync(resource.Resource.Id, "book.pdf", PdfBytes, Manager);
        var key = (await dbContext.LibraryFiles.AsNoTracking().SingleAsync()).StorageKey;
        await service.SetStatusAsync(resource.Resource.Id, "Published", Manager, default);

        await Assert.ThrowsAsync<LibraryException>(() => service.DeleteResourceAsync(resource.Resource.Id, Manager, default));

        await service.SetStatusAsync(resource.Resource.Id, "Archived", Manager, default);
        Assert.True(await service.DeleteResourceAsync(resource.Resource.Id, Manager, default));
        Assert.False(File.Exists(Path.Combine(storageRoot, key)));
    }

    [Fact]
    public async Task FileResourcesCannotBePublishedWithoutAFile()
    {
        var resource = await service.CreateResourceAsync(
            new LibraryResourceInput { Kind = "File", Title = "Gazette", RightsBasis = "PublicDomain" },
            Manager,
            default);

        await Assert.ThrowsAsync<LibraryException>(() => service.SetStatusAsync(resource.Resource.Id, "Published", Manager, default));
    }

    [Fact]
    public async Task CollectionsHideResourcesThatAreNoLongerVisible()
    {
        var shared = await service.CreateResourceAsync(Link("Shared link"), Manager, default);
        await service.SetStatusAsync(shared.Resource.Id, "Published", Manager, default);
        var collection = await service.CreateCollectionAsync(alice, new LibraryCollectionInput { Name = "Economy" }, default);
        Assert.True(await service.AddCollectionItemAsync(alice, collection.Id, shared.Resource.Id, default));
        Assert.Single((await service.GetCollectionAsync(alice, collection.Id, default))!.Items);

        await service.SetStatusAsync(shared.Resource.Id, "Archived", Manager, default);

        Assert.Empty((await service.GetCollectionAsync(alice, collection.Id, default))!.Items);
        Assert.Null(await service.GetCollectionAsync(bob, collection.Id, default));
        await Assert.ThrowsAsync<LibraryException>(() =>
            service.CreateCollectionAsync(alice, new LibraryCollectionInput { Name = "Economy" }, default));
    }

    public void Dispose()
    {
        dbContext.Dispose();
        if (Directory.Exists(storageRoot))
        {
            Directory.Delete(storageRoot, recursive: true);
        }
    }

    private async Task<LibraryFileDto?> UploadAsync(Guid resourceId, string name, byte[] bytes, LibraryActor actor, string role = "Primary")
    {
        await using var stream = new MemoryStream(bytes);
        return await service.UploadFileAsync(resourceId, role, new LibraryUpload(stream, name, bytes.Length), actor, default);
    }

    private static LibraryResourceInput Book(string title, string rights) =>
        new() { Kind = "Book", Category = "Textbook", Title = title, RightsBasis = rights, Book = new LibraryBookInput() };

    private static LibraryResourceInput Link(string title) =>
        new() { Kind = "ExternalLink", Category = "Report", Title = title, Url = "https://mof.gov.bd/report", SourceType = "OfficialGovernment" };

    private static LibraryResourceInput Note(string title) =>
        new() { Kind = "Note", Category = "StudyNote", Title = title, NoteContent = "Summary text" };

    private static LibraryResourceQuery Query() =>
        new(null, null, null, null, null, null, null, "all", false, 1, 20, "recent");

    private void SeedSyllabus()
    {
        var stage = new ExamStage { Id = Guid.NewGuid(), Code = "Written", Name = "Written", LocalizedName = "লিখিত" };
        var subject = new Subject { Id = subjectId, Code = "BA", Name = "Bangladesh Affairs" };
        var published = new SyllabusVersion { Id = Guid.NewGuid(), ExamStage = stage, VersionLabel = "2026", Status = SyllabusVersionStatus.Published };
        var draft = new SyllabusVersion { Id = Guid.NewGuid(), ExamStage = stage, VersionLabel = "Draft", Status = SyllabusVersionStatus.Draft };
        var publishedSubject = new SyllabusSubject { Id = Guid.NewGuid(), SyllabusVersion = published, Subject = subject };
        var draftSubject = new SyllabusSubject { Id = Guid.NewGuid(), SyllabusVersion = draft, Subject = subject };
        dbContext.AddRange(
            new SyllabusItem { Id = publishedTopicId, SyllabusSubject = publishedSubject, Kind = SyllabusItemKind.Topic, Title = "Constitution" },
            new SyllabusItem { Id = draftTopicId, SyllabusSubject = draftSubject, Kind = SyllabusItemKind.Topic, Title = "Draft topic" });
        dbContext.SaveChanges();
    }
}

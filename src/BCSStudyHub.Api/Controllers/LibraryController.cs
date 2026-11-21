using System.Security.Claims;
using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace BCSStudyHub.Api.Controllers;

public static class LibraryRateLimits
{
    public const string UploadPolicy = "library-upload";

    /// <summary>Request ceiling for upload endpoints: the largest accepted file plus multipart overhead.</summary>
    public const long UploadRequestLimit = LibraryFileRules.AbsoluteMaxUploadBytes + 1024 * 1024;
}

/// <summary>Catalog reads. Every library endpoint requires a signed-in user because files are access-controlled.</summary>
[ApiController]
[Route("api/v1/library")]
[Authorize]
public sealed class LibraryController(ILibraryService libraryService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken) =>
        Ok(await libraryService.GetDashboardAsync(LibraryHttp.GetUserId(User), cancellationToken));

    [HttpGet("resources")]
    public async Task<IActionResult> GetResources(
        [FromQuery] string? search,
        [FromQuery] string? kind,
        [FromQuery] string? category,
        [FromQuery] string? format,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? syllabusItemId,
        [FromQuery] string? tag,
        [FromQuery] string scope = "all",
        [FromQuery] bool bookmarkedOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "recent",
        CancellationToken cancellationToken = default) =>
        Ok(await libraryService.GetResourcesAsync(
            new LibraryResourceQuery(
                search,
                kind,
                category,
                format,
                subjectId,
                syllabusItemId,
                tag,
                scope,
                bookmarkedOnly,
                pageNumber,
                pageSize,
                sortBy),
            LibraryHttp.GetUserId(User),
            cancellationToken));

    [HttpGet("resources/{resourceId:guid}")]
    public async Task<IActionResult> GetResource(Guid resourceId, CancellationToken cancellationToken)
    {
        var resource = await libraryService.GetResourceAsync(resourceId, LibraryHttp.GetUserId(User), cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken) =>
        Ok(await libraryService.GetTagsAsync(LibraryHttp.GetUserId(User), cancellationToken));

    [HttpGet("resources/{resourceId:guid}/files/{fileId:guid}/content")]
    public async Task<IActionResult> GetFileContent(
        Guid resourceId,
        Guid fileId,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        var content = await libraryService.OpenFileAsync(resourceId, fileId, LibraryHttp.GetUserId(User), asManager: false, cancellationToken);
        return content is null ? NotFound() : LibraryHttp.FileResponse(this, content, download);
    }
}

/// <summary>The caller's private resources and personal study data.</summary>
[ApiController]
[Route("api/v1/me/library")]
[Authorize]
public sealed class LibraryPersonalController(ILibraryService libraryService) : ControllerBase
{
    private LibraryActor Actor => new(LibraryHttp.GetUserId(User), LibraryManagementScope.Personal);

    [HttpGet("storage")]
    public async Task<IActionResult> GetStorage(CancellationToken cancellationToken) =>
        Ok(await libraryService.GetStorageUsageAsync(Actor.UserId, cancellationToken));

    [HttpGet("resources/{resourceId:guid}")]
    public async Task<IActionResult> GetResource(Guid resourceId, CancellationToken cancellationToken)
    {
        var resource = await libraryService.GetManagedResourceAsync(resourceId, Actor, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpPost("resources")]
    public async Task<IActionResult> CreateResource([FromBody] LibraryResourceInput input, CancellationToken cancellationToken)
    {
        var resource = await libraryService.CreateResourceAsync(input, Actor, cancellationToken);
        return CreatedAtAction(nameof(GetResource), new { resourceId = resource.Resource.Id }, resource);
    }

    [HttpPut("resources/{resourceId:guid}")]
    public async Task<IActionResult> UpdateResource(
        Guid resourceId,
        [FromBody] LibraryResourceInput input,
        CancellationToken cancellationToken)
    {
        var resource = await libraryService.UpdateResourceAsync(resourceId, input, Actor, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpDelete("resources/{resourceId:guid}")]
    public async Task<IActionResult> DeleteResource(Guid resourceId, CancellationToken cancellationToken) =>
        await libraryService.DeleteResourceAsync(resourceId, Actor, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("resources/{resourceId:guid}/files")]
    [EnableRateLimiting(LibraryRateLimits.UploadPolicy)]
    [RequestSizeLimit(LibraryRateLimits.UploadRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LibraryFileRules.AbsoluteMaxUploadBytes)]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadFile(
        Guid resourceId,
        IFormFile? file,
        [FromQuery] string role = "Primary",
        CancellationToken cancellationToken = default) =>
        LibraryHttp.UploadAsync(this, libraryService, resourceId, role, file, Actor, cancellationToken);

    [HttpDelete("resources/{resourceId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid resourceId, Guid fileId, CancellationToken cancellationToken) =>
        await libraryService.DeleteFileAsync(resourceId, fileId, Actor, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("resources/{resourceId:guid}/chapters")]
    public async Task<IActionResult> AddChapter(
        Guid resourceId,
        [FromBody] LibraryChapterInput input,
        CancellationToken cancellationToken)
    {
        var chapter = await libraryService.AddChapterAsync(resourceId, input, Actor, cancellationToken);
        return chapter is null ? NotFound() : Ok(chapter);
    }

    [HttpPut("resources/{resourceId:guid}/chapters/{chapterId:guid}")]
    public async Task<IActionResult> UpdateChapter(
        Guid resourceId,
        Guid chapterId,
        [FromBody] LibraryChapterInput input,
        CancellationToken cancellationToken)
    {
        var chapter = await libraryService.UpdateChapterAsync(resourceId, chapterId, input, Actor, cancellationToken);
        return chapter is null ? NotFound() : Ok(chapter);
    }

    [HttpDelete("resources/{resourceId:guid}/chapters/{chapterId:guid}")]
    public async Task<IActionResult> DeleteChapter(Guid resourceId, Guid chapterId, CancellationToken cancellationToken) =>
        await libraryService.DeleteChapterAsync(resourceId, chapterId, Actor, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await libraryService.GetBookmarksAsync(Actor.UserId, pageNumber, pageSize, cancellationToken));

    [HttpPost("bookmarks")]
    public async Task<IActionResult> AddBookmark([FromBody] LibraryBookmarkInput input, CancellationToken cancellationToken) =>
        Ok(await libraryService.AddBookmarkAsync(Actor.UserId, input, cancellationToken));

    [HttpDelete("bookmarks/{bookmarkId:guid}")]
    public async Task<IActionResult> DeleteBookmark(Guid bookmarkId, CancellationToken cancellationToken) =>
        await libraryService.DeleteBookmarkAsync(Actor.UserId, bookmarkId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await libraryService.GetProgressListAsync(Actor.UserId, status, pageNumber, pageSize, cancellationToken));

    [HttpPut("resources/{resourceId:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(
        Guid resourceId,
        [FromBody] LibraryProgressInput input,
        CancellationToken cancellationToken)
    {
        var progress = await libraryService.UpdateProgressAsync(Actor.UserId, resourceId, input, cancellationToken);
        return progress is null ? NotFound() : Ok(progress);
    }

    [HttpDelete("resources/{resourceId:guid}/progress")]
    public async Task<IActionResult> ClearProgress(Guid resourceId, CancellationToken cancellationToken) =>
        await libraryService.ClearProgressAsync(Actor.UserId, resourceId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("notes")]
    public async Task<IActionResult> GetNotes(
        [FromQuery] Guid? resourceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await libraryService.GetNotesAsync(Actor.UserId, resourceId, pageNumber, pageSize, cancellationToken));

    [HttpPost("resources/{resourceId:guid}/notes")]
    public async Task<IActionResult> CreateNote(
        Guid resourceId,
        [FromBody] LibraryNoteInput input,
        CancellationToken cancellationToken)
    {
        var note = await libraryService.CreateNoteAsync(Actor.UserId, resourceId, input, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpPut("notes/{noteId:guid}")]
    public async Task<IActionResult> UpdateNote(Guid noteId, [FromBody] LibraryNoteInput input, CancellationToken cancellationToken)
    {
        var note = await libraryService.UpdateNoteAsync(Actor.UserId, noteId, input, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpDelete("notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(Guid noteId, CancellationToken cancellationToken) =>
        await libraryService.DeleteNoteAsync(Actor.UserId, noteId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections(CancellationToken cancellationToken) =>
        Ok(await libraryService.GetCollectionsAsync(Actor.UserId, cancellationToken));

    [HttpGet("collections/{collectionId:guid}")]
    public async Task<IActionResult> GetCollection(Guid collectionId, CancellationToken cancellationToken)
    {
        var collection = await libraryService.GetCollectionAsync(Actor.UserId, collectionId, cancellationToken);
        return collection is null ? NotFound() : Ok(collection);
    }

    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection([FromBody] LibraryCollectionInput input, CancellationToken cancellationToken)
    {
        var collection = await libraryService.CreateCollectionAsync(Actor.UserId, input, cancellationToken);
        return CreatedAtAction(nameof(GetCollection), new { collectionId = collection.Id }, collection);
    }

    [HttpPut("collections/{collectionId:guid}")]
    public async Task<IActionResult> UpdateCollection(
        Guid collectionId,
        [FromBody] LibraryCollectionInput input,
        CancellationToken cancellationToken)
    {
        var collection = await libraryService.UpdateCollectionAsync(Actor.UserId, collectionId, input, cancellationToken);
        return collection is null ? NotFound() : Ok(collection);
    }

    [HttpDelete("collections/{collectionId:guid}")]
    public async Task<IActionResult> DeleteCollection(Guid collectionId, CancellationToken cancellationToken) =>
        await libraryService.DeleteCollectionAsync(Actor.UserId, collectionId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("collections/{collectionId:guid}/items")]
    public async Task<IActionResult> AddCollectionItem(
        Guid collectionId,
        [FromBody] LibraryCollectionItemInput input,
        CancellationToken cancellationToken) =>
        await libraryService.AddCollectionItemAsync(Actor.UserId, collectionId, input.ResourceId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpDelete("collections/{collectionId:guid}/items/{resourceId:guid}")]
    public async Task<IActionResult> RemoveCollectionItem(Guid collectionId, Guid resourceId, CancellationToken cancellationToken) =>
        await libraryService.RemoveCollectionItemAsync(Actor.UserId, collectionId, resourceId, cancellationToken)
            ? NoContent()
            : NotFound();
}

/// <summary>Shared library administration. Managers cannot read other users' private resources.</summary>
[ApiController]
[Route("api/v1/admin/library")]
[Authorize(Policy = PermissionCodes.LibraryManage)]
public sealed class AdminLibraryController(ILibraryService libraryService) : ControllerBase
{
    private LibraryActor Actor => new(LibraryHttp.GetUserId(User), LibraryManagementScope.Shared);

    [HttpGet("resources")]
    public async Task<IActionResult> GetResources(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? kind,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await libraryService.GetAdminResourcesAsync(
            new LibraryAdminResourceQuery(search, status, kind, pageNumber, pageSize),
            Actor.UserId,
            cancellationToken));

    [HttpGet("resources/{resourceId:guid}")]
    public async Task<IActionResult> GetResource(Guid resourceId, CancellationToken cancellationToken)
    {
        var resource = await libraryService.GetManagedResourceAsync(resourceId, Actor, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpPost("resources")]
    public async Task<IActionResult> CreateResource([FromBody] LibraryResourceInput input, CancellationToken cancellationToken)
    {
        var resource = await libraryService.CreateResourceAsync(input, Actor, cancellationToken);
        return CreatedAtAction(nameof(GetResource), new { resourceId = resource.Resource.Id }, resource);
    }

    [HttpPut("resources/{resourceId:guid}")]
    public async Task<IActionResult> UpdateResource(
        Guid resourceId,
        [FromBody] LibraryResourceInput input,
        CancellationToken cancellationToken)
    {
        var resource = await libraryService.UpdateResourceAsync(resourceId, input, Actor, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpPost("resources/{resourceId:guid}/publish")]
    public Task<IActionResult> Publish(Guid resourceId, CancellationToken cancellationToken) =>
        SetStatus(resourceId, "Published", cancellationToken);

    [HttpPost("resources/{resourceId:guid}/archive")]
    public Task<IActionResult> Archive(Guid resourceId, CancellationToken cancellationToken) =>
        SetStatus(resourceId, "Archived", cancellationToken);

    [HttpDelete("resources/{resourceId:guid}")]
    public async Task<IActionResult> DeleteResource(Guid resourceId, CancellationToken cancellationToken) =>
        await libraryService.DeleteResourceAsync(resourceId, Actor, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("resources/{resourceId:guid}/files")]
    [EnableRateLimiting(LibraryRateLimits.UploadPolicy)]
    [RequestSizeLimit(LibraryRateLimits.UploadRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LibraryFileRules.AbsoluteMaxUploadBytes)]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadFile(
        Guid resourceId,
        IFormFile? file,
        [FromQuery] string role = "Primary",
        CancellationToken cancellationToken = default) =>
        LibraryHttp.UploadAsync(this, libraryService, resourceId, role, file, Actor, cancellationToken);

    [HttpDelete("resources/{resourceId:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid resourceId, Guid fileId, CancellationToken cancellationToken) =>
        await libraryService.DeleteFileAsync(resourceId, fileId, Actor, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("resources/{resourceId:guid}/files/{fileId:guid}/content")]
    public async Task<IActionResult> GetFileContent(
        Guid resourceId,
        Guid fileId,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        var content = await libraryService.OpenFileAsync(resourceId, fileId, Actor.UserId, asManager: true, cancellationToken);
        return content is null ? NotFound() : LibraryHttp.FileResponse(this, content, download);
    }

    [HttpPost("resources/{resourceId:guid}/chapters")]
    public async Task<IActionResult> AddChapter(
        Guid resourceId,
        [FromBody] LibraryChapterInput input,
        CancellationToken cancellationToken)
    {
        var chapter = await libraryService.AddChapterAsync(resourceId, input, Actor, cancellationToken);
        return chapter is null ? NotFound() : Ok(chapter);
    }

    [HttpPut("resources/{resourceId:guid}/chapters/{chapterId:guid}")]
    public async Task<IActionResult> UpdateChapter(
        Guid resourceId,
        Guid chapterId,
        [FromBody] LibraryChapterInput input,
        CancellationToken cancellationToken)
    {
        var chapter = await libraryService.UpdateChapterAsync(resourceId, chapterId, input, Actor, cancellationToken);
        return chapter is null ? NotFound() : Ok(chapter);
    }

    [HttpDelete("resources/{resourceId:guid}/chapters/{chapterId:guid}")]
    public async Task<IActionResult> DeleteChapter(Guid resourceId, Guid chapterId, CancellationToken cancellationToken) =>
        await libraryService.DeleteChapterAsync(resourceId, chapterId, Actor, cancellationToken) ? NoContent() : NotFound();

    private async Task<IActionResult> SetStatus(Guid resourceId, string status, CancellationToken cancellationToken)
    {
        var resource = await libraryService.SetStatusAsync(resourceId, status, Actor, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }
}

internal static class LibraryHttp
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new LibraryException(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid user identity is required.");
    }

    public static async Task<IActionResult> UploadAsync(
        ControllerBase controller,
        ILibraryService libraryService,
        Guid resourceId,
        string role,
        IFormFile? file,
        LibraryActor actor,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return controller.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid library request",
                detail: "Attach a non-empty file in the 'file' form field.");
        }

        await using var stream = file.OpenReadStream();
        var result = await libraryService.UploadFileAsync(
            resourceId,
            role,
            new LibraryUpload(stream, file.FileName, file.Length),
            actor,
            cancellationToken);
        return result is null ? controller.NotFound() : controller.Ok(result);
    }

    /// <summary>
    /// Streams a stored file. Content type comes from server-side inspection, never from the uploader.
    /// Responses are not cacheable, cannot be MIME-sniffed, and are sandboxed if opened directly.
    /// Office documents are always served as attachments.
    /// </summary>
    public static IActionResult FileResponse(ControllerBase controller, LibraryFileContent content, bool download)
    {
        var headers = controller.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.CacheControl = "private, no-store";
        headers.ContentSecurityPolicy = "default-src 'none'; img-src 'self'; style-src 'unsafe-inline'; sandbox";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        if (download || !content.CanPreviewInline)
        {
            return controller.File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
        }

        headers.ContentDisposition = new ContentDispositionHeaderValue("inline") { FileNameStar = content.FileName }.ToString();
        return controller.File(content.Content, content.ContentType, enableRangeProcessing: true);
    }
}

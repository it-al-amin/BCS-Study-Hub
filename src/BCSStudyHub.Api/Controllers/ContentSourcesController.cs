using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.ContentSources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

[ApiController]
[Route("api/v1/admin/content-sources")]
[Authorize(Policy = PermissionCodes.SourcesManage)]
public sealed class ContentSourcesController(IContentSourceService sources) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] bool? enabled,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await sources.ListAsync(new ContentSourceQuery(search, category, status, enabled, pageNumber, pageSize), cancellationToken));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) => Ok(await sources.GetSummaryAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await sources.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContentSourceInput input, CancellationToken cancellationToken)
    {
        var created = await sources.CreateAsync(input, AdminHttp.UserId(User), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContentSourceInput input, CancellationToken cancellationToken) =>
        Ok(await sources.UpdateAsync(id, input, AdminHttp.UserId(User), cancellationToken));

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken) =>
        Ok(await sources.SetEnabledAsync(id, true, AdminHttp.UserId(User), cancellationToken));

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken) =>
        Ok(await sources.SetEnabledAsync(id, false, AdminHttp.UserId(User), cancellationToken));

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        Ok(await sources.ArchiveAsync(id, AdminHttp.UserId(User), cancellationToken));

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken) =>
        Ok(await sources.RestoreAsync(id, AdminHttp.UserId(User), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sources.DeleteAsync(id, AdminHttp.UserId(User), cancellationToken);
        return NoContent();
    }

    /// <summary>Queues a manual scan. Phase 1 records the request; the scan worker arrives in Phase 4.</summary>
    [HttpPost("{id:guid}/scans")]
    public async Task<IActionResult> RequestScan(Guid id, CancellationToken cancellationToken) =>
        Accepted(await sources.RequestScanAsync(id, AdminHttp.UserId(User), cancellationToken));

    [HttpGet("scans")]
    public async Task<IActionResult> ListScans(
        [FromQuery] Guid? sourceId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await sources.ListScansAsync(sourceId, status, pageNumber, pageSize, cancellationToken));

    [HttpPost("scans/{scanId:guid}/retry")]
    public async Task<IActionResult> RetryScan(Guid scanId, CancellationToken cancellationToken) =>
        Accepted(await sources.RetryScanAsync(scanId, AdminHttp.UserId(User), cancellationToken));
}

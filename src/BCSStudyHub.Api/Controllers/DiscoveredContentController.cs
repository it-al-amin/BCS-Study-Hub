using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.ContentSources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

/// <summary>Content found by source scans. Private: only users who can manage sources may read it.</summary>
[ApiController]
[Route("api/v1/admin/discovered-content")]
[Authorize(Policy = PermissionCodes.SourcesManage)]
public sealed class DiscoveredContentController(IDiscoveredContentService content) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? sourceId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await content.ListAsync(new DiscoveredContentQuery(sourceId, status, search, pageNumber, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await content.GetAsync(id, cancellationToken));
}

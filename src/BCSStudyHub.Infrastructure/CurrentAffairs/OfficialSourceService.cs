using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.CurrentAffairs;

/// <summary>
/// Curated directory of official websites. URLs are verified manually by an editor in a browser and the outcome
/// is recorded; the server deliberately never fetches the URLs itself (no scraping, no server-side request forgery).
/// A source can only be published with a current "Verified" record, and changing its URL voids that record.
/// </summary>
internal sealed class OfficialSourceService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : IOfficialSourceService
{
    public async Task<IReadOnlyList<OfficialSourceDto>> GetPublishedAsync(string? search, string? category, CancellationToken cancellationToken)
    {
        var sources = dbContext.OfficialSources.AsNoTracking().Where(source => source.Status == OfficialSourceStatus.Published);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            if (value.Length > 100)
            {
                throw CurrentAffairsService.Invalid("Search text cannot exceed one hundred characters.");
            }

            sources = sources.Where(source =>
                source.Name.Contains(value) ||
                (source.LocalizedName != null && source.LocalizedName.Contains(value)) ||
                (source.Description != null && source.Description.Contains(value)) ||
                (source.ParentOrganization != null && source.ParentOrganization.Contains(value)) ||
                source.Tags.Any(tag => tag.Tag == value));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var parsed = CurrentAffairsService.Parse<OfficialSourceCategory>(category, "directory category");
            sources = sources.Where(source => source.Category == parsed);
        }

        var rows = await sources.Include(source => source.Tags)
            .OrderBy(source => source.Category).ThenBy(source => source.SortOrder).ThenBy(source => source.Name)
            .Take(500)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<OfficialSourceDto?> GetPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dbContext.OfficialSources.AsNoTracking().Include(candidate => candidate.Tags)
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.Status == OfficialSourceStatus.Published, cancellationToken);
        return source is null ? null : ToDto(source);
    }

    public async Task<PagedResponse<OfficialSourceDto>> GetAdminSourcesAsync(
        string? search,
        string? status,
        string? verificationStatus,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var sources = dbContext.OfficialSources.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            sources = sources.Where(source => source.Name.Contains(value) || source.Url.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = CurrentAffairsService.Parse<OfficialSourceStatus>(status, "status");
            sources = sources.Where(source => source.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(verificationStatus))
        {
            var parsed = CurrentAffairsService.Parse<OfficialSourceVerificationStatus>(verificationStatus, "verification status");
            sources = sources.Where(source => source.VerificationStatus == parsed);
        }

        sources = sources.OrderBy(source => source.Category).ThenBy(source => source.SortOrder).ThenBy(source => source.Name);
        var total = await sources.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await sources.Include(source => source.Tags).Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<OfficialSourceDto>(rows.Select(ToDto).ToArray(), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<OfficialSourceDto?> GetAdminSourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dbContext.OfficialSources.AsNoTracking().Include(candidate => candidate.Tags)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return source is null ? null : ToDto(source);
    }

    public async Task<OfficialSourceDto> CreateAsync(OfficialSourceInput input, Guid actorId, CancellationToken cancellationToken)
    {
        Validate(input);
        await EnsureUniqueUrlAsync(input.Url.Trim(), null, cancellationToken);
        var source = new OfficialSource
        {
            Id = Guid.NewGuid(),
            Status = OfficialSourceStatus.Draft,
            VerificationStatus = OfficialSourceVerificationStatus.Unverified,
        };
        Apply(source, input);
        dbContext.OfficialSources.Add(source);
        AddAudit(actorId, "official-source.created", source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAdminSourceAsync(source.Id, cancellationToken))!;
    }

    public async Task<OfficialSourceDto?> UpdateAsync(Guid id, OfficialSourceInput input, Guid actorId, CancellationToken cancellationToken)
    {
        Validate(input);
        var source = await dbContext.OfficialSources.Include(candidate => candidate.Tags)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var url = input.Url.Trim();
        if (!string.Equals(url, source.Url, StringComparison.Ordinal))
        {
            await EnsureUniqueUrlAsync(url, id, cancellationToken);

            // A verification applies to a specific URL; a new URL must be verified again before it is shown to learners.
            source.VerificationStatus = OfficialSourceVerificationStatus.NeedsReview;
            source.LastVerifiedOn = null;
            source.VerifiedById = null;
            source.VerificationNote = $"URL changed on {CurrentAffairsRules.Today(timeProvider):yyyy-MM-dd}; re-verify before publishing.";
            if (source.Status == OfficialSourceStatus.Published)
            {
                source.Status = OfficialSourceStatus.Draft;
            }
        }

        Apply(source, input);
        source.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorId, "official-source.updated", source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminSourceAsync(source.Id, cancellationToken);
    }

    public async Task<OfficialSourceDto?> RecordVerificationAsync(Guid id, OfficialSourceVerificationInput input, Guid actorId, CancellationToken cancellationToken)
    {
        var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input, new(input), errors, validateAllProperties: true))
        {
            throw CurrentAffairsService.Invalid(errors[0].ErrorMessage ?? "The verification record is not valid.");
        }

        var source = await dbContext.OfficialSources.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var status = CurrentAffairsService.Parse<OfficialSourceVerificationStatus>(input.Status, "verification status");
        source.VerificationStatus = status;
        source.LastVerifiedOn = CurrentAffairsRules.Today(timeProvider);
        source.VerifiedById = actorId;
        source.VerificationNote = input.Note.Trim();
        if (status != OfficialSourceVerificationStatus.Verified && source.Status == OfficialSourceStatus.Published)
        {
            // A source that failed verification is withdrawn from learners until it is fixed and re-verified.
            source.Status = OfficialSourceStatus.Draft;
        }

        AddAudit(actorId, $"official-source.verification.{status.ToString().ToLowerInvariant()}", source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminSourceAsync(source.Id, cancellationToken);
    }

    public async Task<OfficialSourceDto?> SetStatusAsync(Guid id, string status, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await dbContext.OfficialSources.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var newStatus = CurrentAffairsService.Parse<OfficialSourceStatus>(status, "status");
        if (newStatus == OfficialSourceStatus.Draft)
        {
            throw CurrentAffairsService.Invalid("Directory entries can only be published or archived.");
        }

        if (newStatus == OfficialSourceStatus.Published &&
            CurrentAffairsRules.ValidateSourceCanPublish(source.VerificationStatus, source.LastVerifiedOn, CurrentAffairsRules.Today(timeProvider)) is { } error)
        {
            throw CurrentAffairsService.Invalid(error);
        }

        source.Status = newStatus;
        AddAudit(actorId, $"official-source.{newStatus.ToString().ToLowerInvariant()}", source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminSourceAsync(source.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var source = await dbContext.OfficialSources.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (source is null)
        {
            return false;
        }

        if (source.Status == OfficialSourceStatus.Published)
        {
            throw CurrentAffairsService.Invalid("Archive a published directory entry before deleting it.");
        }

        dbContext.OfficialSources.Remove(source);
        AddAudit(actorId, "official-source.deleted", source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Validate(OfficialSourceInput input)
    {
        var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input, new(input), errors, validateAllProperties: true))
        {
            throw CurrentAffairsService.Invalid(errors[0].ErrorMessage ?? "The directory entry is not valid.");
        }
    }

    private async Task EnsureUniqueUrlAsync(string url, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.OfficialSources.AnyAsync(source => source.Url == url && source.Id != exceptId, cancellationToken))
        {
            throw CurrentAffairsService.Conflict("This URL is already in the official resource directory.");
        }
    }

    private static void Apply(OfficialSource source, OfficialSourceInput input)
    {
        source.Name = input.Name.Trim();
        source.LocalizedName = CurrentAffairsService.Normalize(input.LocalizedName);
        source.Category = CurrentAffairsService.Parse<OfficialSourceCategory>(input.Category, "directory category");
        source.Url = input.Url.Trim();
        source.Description = CurrentAffairsService.Normalize(input.Description);
        source.ParentOrganization = CurrentAffairsService.Normalize(input.ParentOrganization);
        source.IsInternational = input.IsInternational;
        source.UsageNotes = CurrentAffairsService.Normalize(input.UsageNotes);
        source.SortOrder = input.SortOrder;
        LibraryRules.TryNormalizeTags(input.Tags, out var tags);
        var tagSet = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in source.Tags.Where(tag => !tagSet.Contains(tag.Tag)).ToArray())
        {
            source.Tags.Remove(existing);
        }

        foreach (var tag in tags.Where(tag => source.Tags.All(existing => !string.Equals(existing.Tag, tag, StringComparison.OrdinalIgnoreCase))))
        {
            source.Tags.Add(new OfficialSourceTag { SourceId = source.Id, Tag = tag });
        }
    }

    internal static OfficialSourceDto ToDto(OfficialSource source) =>
        new(
            source.Id,
            source.Name,
            source.LocalizedName,
            source.Category.ToString(),
            source.Url,
            source.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
            source.Description,
            source.ParentOrganization,
            source.IsInternational,
            source.UsageNotes,
            source.VerificationStatus.ToString(),
            source.LastVerifiedOn,
            source.VerificationNote,
            source.Status.ToString(),
            source.SortOrder,
            source.Tags.Select(tag => tag.Tag).OrderBy(tag => tag).ToArray(),
            source.UpdatedAt);

    private void AddAudit(Guid actorId, string action, Guid entityId) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = nameof(OfficialSource),
            EntityId = entityId.ToString(),
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier,
        });
}

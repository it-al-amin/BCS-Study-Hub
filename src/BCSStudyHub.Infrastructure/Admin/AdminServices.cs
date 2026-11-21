using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Admin;
using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Admin;

internal static class AdminAudit
{
    public static void Add(ApplicationDbContext dbContext, IHttpContextAccessor accessor, Guid actorId, string action, string entityType, string entityId) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.Length <= 100 ? entityId : entityId[..100],
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = accessor.HttpContext?.TraceIdentifier,
        });

    public static void Validate(object model)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            throw new AdminException(StatusCodes.Status400BadRequest, "Invalid request", results[0].ErrorMessage ?? "The request is not valid.");
        }
    }
}

internal sealed class SiteSettingsService(ApplicationDbContext dbContext, IHttpContextAccessor accessor, TimeProvider timeProvider) : ISiteSettingsService
{
    public async Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken cancellationToken)
    {
        var settings = await LoadAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var announcementActive = settings.AnnouncementEnabled &&
                                 !string.IsNullOrWhiteSpace(settings.AnnouncementText) &&
                                 (settings.AnnouncementStartsAt is null || settings.AnnouncementStartsAt <= now) &&
                                 (settings.AnnouncementEndsAt is null || settings.AnnouncementEndsAt > now);
        return new PublicSiteSettingsDto(
            settings.SiteName,
            settings.Tagline,
            announcementActive ? settings.AnnouncementText : null,
            settings.AnnouncementTone.ToString(),
            settings.SupportEmail,
            settings.RegistrationOpen,
            settings.FooterNotice);
    }

    public async Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken) => ToDto(await LoadAsync(cancellationToken));

    public async Task<SiteSettingsDto> UpdateAsync(SiteSettingsInput input, Guid actorId, CancellationToken cancellationToken)
    {
        AdminAudit.Validate(input);
        var settings = await dbContext.SiteSettings.SingleOrDefaultAsync(row => row.Id == SiteSettings.SingletonId, cancellationToken);
        if (settings is null)
        {
            settings = new SiteSettings();
            dbContext.SiteSettings.Add(settings);
        }

        settings.SiteName = input.SiteName.Trim();
        settings.Tagline = Normalize(input.Tagline);
        settings.AnnouncementEnabled = input.AnnouncementEnabled;
        settings.AnnouncementText = Normalize(input.AnnouncementText);
        settings.AnnouncementTone = Enum.Parse<AnnouncementTone>(input.AnnouncementTone, true);
        settings.AnnouncementStartsAt = input.AnnouncementStartsAt;
        settings.AnnouncementEndsAt = input.AnnouncementEndsAt;
        settings.SupportEmail = Normalize(input.SupportEmail);
        settings.RegistrationOpen = input.RegistrationOpen;
        settings.FooterNotice = Normalize(input.FooterNotice);
        settings.UpdatedById = actorId;
        settings.UpdatedAt = timeProvider.GetUtcNow();
        AdminAudit.Add(dbContext, accessor, actorId, "site-settings.updated", nameof(SiteSettings), SiteSettings.SingletonId.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<bool> IsRegistrationOpenAsync(CancellationToken cancellationToken) => (await LoadAsync(cancellationToken)).RegistrationOpen;

    private async Task<SiteSettings> LoadAsync(CancellationToken cancellationToken) =>
        await dbContext.SiteSettings.AsNoTracking().SingleOrDefaultAsync(row => row.Id == SiteSettings.SingletonId, cancellationToken) ?? new SiteSettings();

    private static SiteSettingsDto ToDto(SiteSettings settings) =>
        new(settings.SiteName, settings.Tagline, settings.AnnouncementEnabled, settings.AnnouncementText, settings.AnnouncementTone.ToString(),
            settings.AnnouncementStartsAt, settings.AnnouncementEndsAt, settings.SupportEmail, settings.RegistrationOpen, settings.FooterNotice,
            settings.UpdatedAt == default ? null : settings.UpdatedAt);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal sealed class AuditQueryService(ApplicationDbContext dbContext) : IAuditQueryService
{
    public async Task<PagedResponse<AuditEntryDto>> GetAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        var logs = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var type = query.EntityType.Trim();
            logs = logs.Where(log => log.EntityType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var id = query.EntityId.Trim();
            logs = logs.Where(log => log.EntityId == id);
        }

        if (query.ActorId is Guid actor)
        {
            logs = logs.Where(log => log.ActorId == actor);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            logs = logs.Where(log => log.Action.StartsWith(action));
        }

        if (query.From is DateOnly from)
        {
            var start = BangladeshTime.StartOf(from);
            logs = logs.Where(log => log.OccurredAt >= start);
        }

        if (query.To is DateOnly to)
        {
            var end = BangladeshTime.StartOf(to.AddDays(1));
            logs = logs.Where(log => log.OccurredAt < end);
        }

        logs = logs.OrderByDescending(log => log.OccurredAt).ThenByDescending(log => log.Id);
        var total = await logs.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await logs.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<AuditEntryDto>(await ToDtosAsync(dbContext, rows, cancellationToken), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken cancellationToken) =>
        await dbContext.AuditLogs.AsNoTracking().Where(log => log.EntityType != null).Select(log => log.EntityType!).Distinct().OrderBy(type => type).ToListAsync(cancellationToken);

    internal static async Task<IReadOnlyList<AuditEntryDto>> ToDtosAsync(ApplicationDbContext dbContext, IReadOnlyList<AuditLog> rows, CancellationToken cancellationToken)
    {
        var actorIds = rows.Where(row => row.ActorId != null).Select(row => row.ActorId!.Value).Distinct().ToArray();
        var actors = actorIds.Length == 0
            ? []
            : await dbContext.Users.AsNoTracking().Where(user => actorIds.Contains(user.Id))
                .Select(user => new { user.Id, user.DisplayName, user.Email }).ToDictionaryAsync(user => user.Id, cancellationToken);
        return rows.Select(row =>
        {
            var actor = row.ActorId is Guid id && actors.TryGetValue(id, out var found) ? found : null;
            return new AuditEntryDto(row.Id, row.OccurredAt, row.Action, row.EntityType, row.EntityId, row.ActorId, actor?.DisplayName, actor?.Email, row.CorrelationId);
        }).ToArray();
    }
}

internal sealed class AdminOverviewService(ApplicationDbContext dbContext, TimeProvider timeProvider) : IAdminOverviewService
{
    public async Task<AdminOverviewDto> GetAsync(IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var modules = new List<ModuleOverviewDto>();
        var today = BangladeshTime.Today(timeProvider);

        if (permissions.Contains(PermissionCodes.SyllabusManage))
        {
            var versions = await Count(dbContext.SyllabusVersions.Select(row => row.Status), cancellationToken);
            var unverified = await dbContext.SyllabusItems.CountAsync(item => item.VerificationStatus != SyllabusVerificationStatus.Verified, cancellationToken);
            modules.Add(new("Syllabus", "Syllabus versions", PermissionCodes.SyllabusManage, "/admin/syllabus",
                versions.Get(SyllabusVersionStatus.Draft), versions.Get(SyllabusVersionStatus.Published), versions.Get(SyllabusVersionStatus.Archived), unverified, "topics not yet source-verified"));
        }

        if (permissions.Contains(PermissionCodes.McqManage))
        {
            var questions = await Count(dbContext.McqQuestions.Select(row => row.Status), cancellationToken);
            var sets = await Count(dbContext.McqExamSets.Select(row => row.Status), cancellationToken);
            modules.Add(new("McqQuestions", "MCQ questions", PermissionCodes.McqManage, "/admin/mcq",
                questions.Get(McqContentStatus.Draft), questions.Get(McqContentStatus.Published), questions.Get(McqContentStatus.Archived), questions.Get(McqContentStatus.Draft), "drafts awaiting review"));
            modules.Add(new("McqExamSets", "MCQ exam sets", PermissionCodes.McqManage, "/admin/mcq",
                sets.Get(McqContentStatus.Draft), sets.Get(McqContentStatus.Published), sets.Get(McqContentStatus.Archived), sets.Get(McqContentStatus.Draft), "drafts"));
        }

        if (permissions.Contains(PermissionCodes.WrittenManage))
        {
            var questions = await Count(dbContext.WrittenQuestions.Select(row => row.Status), cancellationToken);
            var draftAnswers = await dbContext.WrittenModelAnswers.CountAsync(answer => answer.Status == WrittenModelAnswerStatus.Draft, cancellationToken);
            modules.Add(new("WrittenQuestions", "Written questions", PermissionCodes.WrittenManage, "/admin/written",
                questions.Get(WrittenQuestionStatus.Draft), questions.Get(WrittenQuestionStatus.Published), questions.Get(WrittenQuestionStatus.Archived), draftAnswers, "model answers in draft"));
        }

        if (permissions.Contains(PermissionCodes.VivaManage))
        {
            var questions = await Count(dbContext.VivaQuestions.Select(row => row.Status), cancellationToken);
            modules.Add(new("VivaQuestions", "Viva questions", PermissionCodes.VivaManage, "/admin/viva",
                questions.Get(VivaQuestionStatus.Draft), questions.Get(VivaQuestionStatus.Published), questions.Get(VivaQuestionStatus.Archived), questions.Get(VivaQuestionStatus.Draft), "drafts awaiting review"));
        }

        if (permissions.Contains(PermissionCodes.LibraryManage))
        {
            var resources = await Count(dbContext.LibraryResources.Where(row => row.Visibility == LibraryVisibility.Shared).Select(row => row.Status), cancellationToken);
            var noRights = await dbContext.LibraryResources.CountAsync(resource =>
                resource.Visibility == LibraryVisibility.Shared && resource.Status == LibraryResourceStatus.Draft && resource.Files.Any() &&
                resource.RightsBasis == LibraryRightsBasis.NotApplicable, cancellationToken);
            modules.Add(new("LibraryResources", "Shared library", PermissionCodes.LibraryManage, "/admin/library",
                resources.Get(LibraryResourceStatus.Draft), resources.Get(LibraryResourceStatus.Published), resources.Get(LibraryResourceStatus.Archived), noRights, "drafts with files but no rights basis"));
        }

        if (permissions.Contains(PermissionCodes.CurrentAffairsManage))
        {
            var entries = await Count(dbContext.CurrentAffairsEntries.Select(row => row.Status), cancellationToken);
            var unverifiedEntries = await dbContext.CurrentAffairsEntries.CountAsync(entry =>
                entry.Status == CurrentAffairsStatus.Draft && entry.SourceStatus == CurrentAffairsSourceStatus.Unverified, cancellationToken);
            modules.Add(new("CurrentAffairs", "Current affairs", PermissionCodes.CurrentAffairsManage, "/admin/current-affairs",
                entries.Get(CurrentAffairsStatus.Draft), entries.Get(CurrentAffairsStatus.Published), entries.Get(CurrentAffairsStatus.Archived), unverifiedEntries, "drafts with unverified sources"));

            var sources = await Count(dbContext.OfficialSources.Select(row => row.Status), cancellationToken);
            var staleBefore = today.AddDays(-CurrentAffairsRules.MaxVerificationAgeDays);
            var needsVerification = await dbContext.OfficialSources.CountAsync(source =>
                source.Status != OfficialSourceStatus.Archived &&
                (source.VerificationStatus != OfficialSourceVerificationStatus.Verified || source.LastVerifiedOn == null || source.LastVerifiedOn < staleBefore), cancellationToken);
            modules.Add(new("OfficialSources", "Official source directory", PermissionCodes.CurrentAffairsManage, "/admin/official-sources",
                sources.Get(OfficialSourceStatus.Draft), sources.Get(OfficialSourceStatus.Published), sources.Get(OfficialSourceStatus.Archived), needsVerification, "need manual URL verification"));
        }

        var thirtyDaysAgo = timeProvider.GetUtcNow().AddDays(-30);
        var recent = permissions.Contains(PermissionCodes.AuditRead)
            ? await AuditQueryService.ToDtosAsync(dbContext, await dbContext.AuditLogs.AsNoTracking().OrderByDescending(log => log.OccurredAt).ThenByDescending(log => log.Id).Take(12).ToListAsync(cancellationToken), cancellationToken)
            : [];
        return new AdminOverviewDto(
            modules,
            await dbContext.Users.CountAsync(cancellationToken),
            await dbContext.Users.CountAsync(user => user.CreatedAt >= thirtyDaysAgo, cancellationToken),
            recent);
    }

    private static async Task<StatusCounts> Count<TStatus>(IQueryable<TStatus> statuses, CancellationToken cancellationToken) where TStatus : struct, Enum
    {
        var rows = await statuses.GroupBy(status => status).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
        return new StatusCounts(rows.ToDictionary(row => row.Key.ToString(), row => row.Count));
    }

    private sealed record StatusCounts(IReadOnlyDictionary<string, int> Values)
    {
        public int Get<TStatus>(TStatus status) where TStatus : struct, Enum => Values.GetValueOrDefault(status.ToString());
    }
}

/// <summary>
/// Cross-module tag maintenance. Tags are composite-key rows, so a rename deletes the old row and inserts the new
/// one; when an item already has the target tag the old row is simply removed (a merge).
/// </summary>
internal sealed class TagAdminService(ApplicationDbContext dbContext, IHttpContextAccessor accessor) : ITagAdminService
{
    public async Task<IReadOnlyList<TagUsageDto>> GetAsync(IReadOnlySet<TagModule> modules, string? search, CancellationToken cancellationToken)
    {
        var term = search?.Trim().ToLowerInvariant();
        var counts = new List<(string Tag, TagModule Module, int Count)>();
        foreach (var module in modules)
        {
            var rows = module switch
            {
                TagModule.Mcq => await Group(dbContext.McqQuestionTags.Select(tag => tag.Tag), cancellationToken),
                TagModule.Written => await Group(dbContext.WrittenQuestionTags.Select(tag => tag.Tag), cancellationToken),
                TagModule.Library => await Group(dbContext.LibraryResourceTags.Select(tag => tag.Tag), cancellationToken),
                TagModule.CurrentAffairs => await Group(dbContext.CurrentAffairsEntryTags.Select(tag => tag.Tag), cancellationToken),
                TagModule.OfficialSources => await Group(dbContext.OfficialSourceTags.Select(tag => tag.Tag), cancellationToken),
                _ => [],
            };
            counts.AddRange(rows.Select(row => (row.Tag, module, row.Count)));
        }

        return counts
            .Where(row => term is null || row.Tag.ToLowerInvariant().Contains(term))
            .GroupBy(row => row.Tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TagUsageDto(
                group.First().Tag,
                group.GroupBy(row => row.Module).ToDictionary(item => item.Key.ToString(), item => item.Sum(row => row.Count)),
                group.Sum(row => row.Count)))
            .OrderByDescending(tag => tag.Total)
            .ThenBy(tag => tag.Tag, StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToArray();
    }

    public async Task<TagChangeResultDto> RenameAsync(IReadOnlySet<TagModule> modules, TagRenameInput input, Guid actorId, CancellationToken cancellationToken)
    {
        AdminAudit.Validate(input);
        var from = input.From.Trim();
        var to = input.To.Trim();
        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            throw new AdminException(StatusCodes.Status400BadRequest, "Invalid request", "The new tag must differ from the old one.");
        }

        var total = new Counter();
        foreach (var module in modules)
        {
            switch (module)
            {
                case TagModule.Mcq:
                    await RenameIn(dbContext.McqQuestionTags, tag => tag.QuestionId, tag => tag.Tag, (owner, value) => new McqQuestionTag { QuestionId = owner, Tag = value }, from, to, total, cancellationToken);
                    break;
                case TagModule.Written:
                    await RenameIn(dbContext.WrittenQuestionTags, tag => tag.QuestionId, tag => tag.Tag, (owner, value) => new WrittenQuestionTag { QuestionId = owner, Tag = value }, from, to, total, cancellationToken);
                    break;
                case TagModule.Library:
                    await RenameIn(dbContext.LibraryResourceTags, tag => tag.ResourceId, tag => tag.Tag, (owner, value) => new LibraryResourceTag { ResourceId = owner, Tag = value }, from, to, total, cancellationToken);
                    break;
                case TagModule.CurrentAffairs:
                    await RenameIn(dbContext.CurrentAffairsEntryTags, tag => tag.EntryId, tag => tag.Tag, (owner, value) => new CurrentAffairsEntryTag { EntryId = owner, Tag = value }, from, to, total, cancellationToken);
                    break;
                case TagModule.OfficialSources:
                    await RenameIn(dbContext.OfficialSourceTags, tag => tag.SourceId, tag => tag.Tag, (owner, value) => new OfficialSourceTag { SourceId = owner, Tag = value }, from, to, total, cancellationToken);
                    break;
            }
        }

        AdminAudit.Add(dbContext, accessor, actorId, "tags.renamed", "Tag", $"{from} -> {to}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TagChangeResultDto(to, total.Updated, total.Merged, 0);
    }

    public async Task<TagChangeResultDto> DeleteAsync(IReadOnlySet<TagModule> modules, TagDeleteInput input, Guid actorId, CancellationToken cancellationToken)
    {
        AdminAudit.Validate(input);
        var tag = input.Tag.Trim();
        var lower = tag.ToLowerInvariant();
        var removed = 0;
        foreach (var module in modules)
        {
            removed += module switch
            {
                TagModule.Mcq => Remove(dbContext.McqQuestionTags, await dbContext.McqQuestionTags.Where(row => row.Tag.ToLower() == lower).ToListAsync(cancellationToken)),
                TagModule.Written => Remove(dbContext.WrittenQuestionTags, await dbContext.WrittenQuestionTags.Where(row => row.Tag.ToLower() == lower).ToListAsync(cancellationToken)),
                TagModule.Library => Remove(dbContext.LibraryResourceTags, await dbContext.LibraryResourceTags.Where(row => row.Tag.ToLower() == lower).ToListAsync(cancellationToken)),
                TagModule.CurrentAffairs => Remove(dbContext.CurrentAffairsEntryTags, await dbContext.CurrentAffairsEntryTags.Where(row => row.Tag.ToLower() == lower).ToListAsync(cancellationToken)),
                TagModule.OfficialSources => Remove(dbContext.OfficialSourceTags, await dbContext.OfficialSourceTags.Where(row => row.Tag.ToLower() == lower).ToListAsync(cancellationToken)),
                _ => 0,
            };
        }

        AdminAudit.Add(dbContext, accessor, actorId, "tags.deleted", "Tag", tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TagChangeResultDto(tag, 0, 0, removed);
    }

    private sealed class Counter
    {
        public int Updated { get; set; }
        public int Merged { get; set; }
    }

    private static async Task<List<(string Tag, int Count)>> Group(IQueryable<string> tags, CancellationToken cancellationToken) =>
        (await tags.GroupBy(tag => tag).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(cancellationToken))
            .Select(row => (row.Key, row.Count)).ToList();

    private static int Remove<T>(DbSet<T> set, List<T> rows) where T : class
    {
        set.RemoveRange(rows);
        return rows.Count;
    }

    private static async Task RenameIn<T>(
        DbSet<T> set,
        Func<T, Guid> owner,
        Func<T, string> tagOf,
        Func<Guid, string, T> create,
        string from,
        string to,
        Counter counter,
        CancellationToken cancellationToken)
        where T : class
    {
        var fromLower = from.ToLowerInvariant();
        var toLower = to.ToLowerInvariant();
        var rows = await set.Where(row => EF.Property<string>(row, "Tag").ToLower() == fromLower || EF.Property<string>(row, "Tag").ToLower() == toLower)
            .ToListAsync(cancellationToken);
        var caseOnly = fromLower == toLower;
        foreach (var group in rows.GroupBy(owner))
        {
            var sources = group.Where(row => string.Equals(tagOf(row), from, StringComparison.OrdinalIgnoreCase) && (!caseOnly || tagOf(row) != to)).ToArray();
            if (sources.Length == 0)
            {
                continue;
            }

            var hasTarget = !caseOnly && group.Any(row => string.Equals(tagOf(row), to, StringComparison.OrdinalIgnoreCase));
            set.RemoveRange(sources);
            if (hasTarget)
            {
                counter.Merged++;
            }
            else
            {
                set.Add(create(group.Key, to));
                counter.Updated++;
            }
        }
    }
}

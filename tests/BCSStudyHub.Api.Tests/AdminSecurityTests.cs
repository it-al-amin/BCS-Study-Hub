using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BCSStudyHub.Api.Authorization;
using BCSStudyHub.Api.Controllers;
using BCSStudyHub.Application.Admin;
using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Admin;
using BCSStudyHub.Infrastructure.Identity;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Api.Tests;

/// <summary>Server-side authorization is verified structurally, so a new admin endpoint cannot ship unprotected.</summary>
public sealed class AdminAuthorizationCoverageTests
{
    private static readonly Type[] Controllers = typeof(AdminConsoleController).Assembly.GetTypes()
        .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
        .ToArray();

    private static string? RouteOf(Type controller) => controller.GetCustomAttribute<RouteAttribute>()?.Template;

    [Fact]
    public void EveryAdminControllerRequiresARegisteredPermissionPolicy()
    {
        var admin = Controllers.Where(type => RouteOf(type)?.StartsWith("api/v1/admin", StringComparison.OrdinalIgnoreCase) == true).ToArray();
        Assert.NotEmpty(admin);
        Assert.All(admin, controller =>
        {
            var authorize = controller.GetCustomAttribute<AuthorizeAttribute>();
            Assert.True(authorize?.Policy is not null, $"{controller.Name} must declare [Authorize(Policy = ...)].");
            Assert.Contains(authorize!.Policy!, PermissionCodes.Descriptions.Keys);
            Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.All(controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                action => Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>()));
        });
    }

    [Fact]
    public void EveryPersonalControllerRequiresAuthentication()
    {
        var personal = Controllers.Where(type => RouteOf(type)?.StartsWith("api/v1/me", StringComparison.OrdinalIgnoreCase) == true).ToArray();
        Assert.NotEmpty(personal);
        Assert.All(personal, controller =>
        {
            Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
        });
    }

    [Fact]
    public void EveryPermissionHasAClaimPolicy()
    {
        var options = new AuthorizationOptions();
        PermissionPolicies.Register(options);

        foreach (var code in PermissionCodes.Descriptions.Keys)
        {
            var policy = options.GetPolicy(code);
            Assert.NotNull(policy);
            var claim = Assert.Single(policy!.Requirements.OfType<ClaimsAuthorizationRequirement>());
            Assert.Equal(PermissionPolicies.ClaimType, claim.ClaimType);
            Assert.Equal([code], claim.AllowedValues);
            Assert.Contains(policy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        }
    }

    [Fact]
    public void BulkAndTagModulesMapToRealPermissions()
    {
        Assert.All(AdminConsoleController.BulkModulePermissions.Values, code => Assert.Contains(code, PermissionCodes.Descriptions.Keys));
        Assert.All(TagModules.Permissions.Values, code => Assert.Contains(code, PermissionCodes.Descriptions.Keys));
    }

    [Fact]
    public void TagModulesAreLimitedToTheCallersPermissions()
    {
        var mcqOnly = TagModules.Allowed([PermissionCodes.McqManage], null);
        Assert.Equal([TagModule.Mcq], mcqOnly);

        var forbidden = Assert.Throws<AdminException>(() => TagModules.Allowed([PermissionCodes.McqManage], "Library"));
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Throws<AdminException>(() => TagModules.Allowed([PermissionCodes.McqManage], "Everything"));
        Assert.Empty(TagModules.Allowed([], null));
    }

    [Fact]
    public void BulkInputIsBoundedAndValidated()
    {
        Assert.False(IsValid(new BulkStatusInput { Module = "McqQuestions", Action = "Delete", Ids = [Guid.NewGuid()] }));
        Assert.False(IsValid(new BulkStatusInput { Module = "McqQuestions", Action = "Publish", Ids = [] }));
        Assert.False(IsValid(new BulkStatusInput { Module = "McqQuestions", Action = "Publish", Ids = Enumerable.Range(0, BulkLimits.MaxItems + 1).Select(_ => Guid.NewGuid()).ToArray() }));
        var id = Guid.NewGuid();
        Assert.False(IsValid(new BulkStatusInput { Module = "McqQuestions", Action = "Archive", Ids = [id, id] }));
        Assert.True(IsValid(new BulkStatusInput { Module = "McqQuestions", Action = "Archive", Ids = [id] }));
    }

    internal static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}

public sealed class AdminServicesTests : IDisposable
{
    private readonly ApplicationDbContext dbContext = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly FixedTimeProvider clock = new(new DateTimeOffset(2026, 11, 21, 6, 0, 0, TimeSpan.Zero));
    private readonly Guid admin = Guid.NewGuid();

    [Fact]
    public async Task SiteSettingsDefaultAndValidate()
    {
        var service = new SiteSettingsService(dbContext, new HttpContextAccessor(), clock);
        var defaults = await service.GetPublicAsync(default);
        Assert.Equal("BCS Study Hub", defaults.SiteName);
        Assert.True(defaults.RegistrationOpen);
        Assert.Null(defaults.Announcement);

        await Assert.ThrowsAsync<AdminException>(() => service.UpdateAsync(new SiteSettingsInput { SiteName = "Hub", AnnouncementEnabled = true }, admin, default));
        await Assert.ThrowsAsync<AdminException>(() => service.UpdateAsync(new SiteSettingsInput { SiteName = "Hub", SupportEmail = "not-an-email" }, admin, default));
        await Assert.ThrowsAsync<AdminException>(() => service.UpdateAsync(new SiteSettingsInput { SiteName = "Hub", AnnouncementTone = "Loud" }, admin, default));
        await Assert.ThrowsAsync<AdminException>(() => service.UpdateAsync(new SiteSettingsInput
        {
            SiteName = "Hub", AnnouncementStartsAt = clock.Now, AnnouncementEndsAt = clock.Now.AddHours(-1),
        }, admin, default));
    }

    [Fact]
    public async Task AnnouncementsShowOnlyInsideTheirWindowAndRegistrationCanBeClosed()
    {
        var service = new SiteSettingsService(dbContext, new HttpContextAccessor(), clock);
        await service.UpdateAsync(new SiteSettingsInput
        {
            SiteName = "BCS Study Hub",
            AnnouncementEnabled = true,
            AnnouncementText = "Maintenance tonight",
            AnnouncementTone = "Warning",
            AnnouncementStartsAt = clock.Now.AddHours(1),
            AnnouncementEndsAt = clock.Now.AddHours(3),
            RegistrationOpen = false,
        }, admin, default);

        Assert.Null((await service.GetPublicAsync(default)).Announcement);
        clock.Now = clock.Now.AddHours(2);
        Assert.Equal("Maintenance tonight", (await service.GetPublicAsync(default)).Announcement);
        clock.Now = clock.Now.AddHours(2);
        Assert.Null((await service.GetPublicAsync(default)).Announcement);

        Assert.False(await service.IsRegistrationOpenAsync(default));
        Assert.Contains(dbContext.AuditLogs, log => log.Action == "site-settings.updated" && log.ActorId == admin);
    }

    [Fact]
    public async Task TagRenameMergesAndRespectsModuleScope()
    {
        var mcqA = Guid.NewGuid();
        var mcqB = Guid.NewGuid();
        var entry = Guid.NewGuid();
        dbContext.McqQuestionTags.AddRange(
            new McqQuestionTag { QuestionId = mcqA, Tag = "budget" },
            new McqQuestionTag { QuestionId = mcqB, Tag = "budget" },
            new McqQuestionTag { QuestionId = mcqB, Tag = "Budget 2026" });
        dbContext.CurrentAffairsEntryTags.Add(new CurrentAffairsEntryTag { EntryId = entry, Tag = "budget" });
        await dbContext.SaveChangesAsync();
        var service = new TagAdminService(dbContext, new HttpContextAccessor());

        var result = await service.RenameAsync(new HashSet<TagModule> { TagModule.Mcq }, new TagRenameInput { From = "budget", To = "Budget 2026" }, admin, default);

        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.Merged);
        var mcqTags = await dbContext.McqQuestionTags.AsNoTracking().ToListAsync();
        Assert.Equal(2, mcqTags.Count);
        Assert.All(mcqTags, tag => Assert.Equal("Budget 2026", tag.Tag));
        Assert.Equal("budget", (await dbContext.CurrentAffairsEntryTags.AsNoTracking().SingleAsync()).Tag);

        var usage = await service.GetAsync(new HashSet<TagModule> { TagModule.Mcq, TagModule.CurrentAffairs }, null, default);
        Assert.Equal(2, usage.Single(tag => tag.Tag == "Budget 2026").Total);

        var removed = await service.DeleteAsync(new HashSet<TagModule> { TagModule.CurrentAffairs }, new TagDeleteInput { Tag = "BUDGET" }, admin, default);
        Assert.Equal(1, removed.Removed);
        Assert.Empty(dbContext.CurrentAffairsEntryTags);
        Assert.Equal(2, dbContext.AuditLogs.Count(log => log.EntityType == "Tag"));
        await Assert.ThrowsAsync<AdminException>(() => service.RenameAsync(new HashSet<TagModule> { TagModule.Mcq }, new TagRenameInput { From = "x", To = "x" }, admin, default));
    }

    [Fact]
    public async Task AuditQueriesFilterAndNameActors()
    {
        dbContext.Users.Add(new ApplicationUser { Id = admin, UserName = "admin@example.org", Email = "admin@example.org", DisplayName = "Site Admin" });
        dbContext.AuditLogs.AddRange(
            new AuditLog { ActorId = admin, Action = "mcq.question.published", EntityType = "McqQuestion", EntityId = "q1", OccurredAt = clock.Now.AddDays(-1) },
            new AuditLog { ActorId = admin, Action = "mcq.question.archived", EntityType = "McqQuestion", EntityId = "q1", OccurredAt = clock.Now },
            new AuditLog { ActorId = Guid.NewGuid(), Action = "library.shared.resource.created", EntityType = "LibraryResource", EntityId = "r1", OccurredAt = clock.Now });
        await dbContext.SaveChangesAsync();
        var service = new AuditQueryService(dbContext);

        var history = await service.GetAsync(new AuditQuery("McqQuestion", "q1", null, null, null, null, 1, 50), default);
        Assert.Equal(["mcq.question.archived", "mcq.question.published"], history.Items.Select(item => item.Action).ToArray());
        Assert.All(history.Items, item => Assert.Equal("Site Admin", item.ActorName));
        Assert.Single((await service.GetAsync(new AuditQuery(null, null, null, "library.", null, null, 1, 50), default)).Items);
        Assert.Equal(2, (await service.GetAsync(new AuditQuery(null, null, null, null, new DateOnly(2026, 11, 21), new DateOnly(2026, 11, 21), 1, 50), default)).TotalCount);
        Assert.Equal(["LibraryResource", "McqQuestion"], await service.GetEntityTypesAsync(default));
    }

    [Fact]
    public async Task OverviewIsLimitedToTheCallersModules()
    {
        dbContext.McqQuestions.AddRange(
            new McqQuestion { Id = Guid.NewGuid(), Prompt = "a", Status = McqContentStatus.Draft },
            new McqQuestion { Id = Guid.NewGuid(), Prompt = "b", Status = McqContentStatus.Published });
        dbContext.OfficialSources.Add(new OfficialSource { Id = Guid.NewGuid(), Name = "BPSC", Url = "https://bpsc.gov.bd/", Status = OfficialSourceStatus.Draft });
        dbContext.AuditLogs.Add(new AuditLog { ActorId = admin, Action = "x", OccurredAt = clock.Now });
        await dbContext.SaveChangesAsync();
        var service = new AdminOverviewService(dbContext, clock);

        var mcqEditor = await service.GetAsync(new HashSet<string> { PermissionCodes.McqManage, PermissionCodes.ContentManage }, default);
        Assert.Equal(["McqQuestions", "McqExamSets"], mcqEditor.Modules.Select(module => module.Module).ToArray());
        Assert.Equal((1, 1), (mcqEditor.Modules[0].Draft, mcqEditor.Modules[0].Published));
        Assert.Empty(mcqEditor.RecentActivity);

        var full = await service.GetAsync(PermissionCodes.Descriptions.Keys.ToHashSet(), default);
        Assert.Equal(1, full.Modules.Single(module => module.Module == "OfficialSources").Attention);
        Assert.Single(full.RecentActivity);
    }

    public void Dispose() => dbContext.Dispose();
}

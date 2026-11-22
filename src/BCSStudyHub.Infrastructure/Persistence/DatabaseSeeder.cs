using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCSStudyHub.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext dbContext,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissions = PermissionCodes.Descriptions;
        var permissionEntities = new Dictionary<string, Permission>(StringComparer.Ordinal);

        foreach (var (code, description) in permissions)
        {
            var permission = await dbContext.Permissions
                .SingleOrDefaultAsync(item => item.Code == code, cancellationToken);
            if (permission is null)
            {
                permission = new Permission { Id = Guid.NewGuid(), Code = code, Description = description };
                dbContext.Permissions.Add(permission);
            }
            else if (!string.Equals(permission.Description, description, StringComparison.Ordinal))
            {
                permission.Description = description;
            }

            permissionEntities.Add(code, permission);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rolePermissions = new Dictionary<string, string[]>
        {
            ["Candidate"] = [PermissionCodes.DashboardRead, PermissionCodes.SyllabusRead],
            ["ContentManager"] =
            [
                PermissionCodes.DashboardRead,
                PermissionCodes.SyllabusRead,
                PermissionCodes.ContentManage,
                PermissionCodes.SyllabusManage,
                PermissionCodes.McqManage,
                PermissionCodes.WrittenManage,
                PermissionCodes.VivaManage,
                PermissionCodes.LibraryManage,
                PermissionCodes.CurrentAffairsManage,
                PermissionCodes.SourcesManage,
            ],
            ["Administrator"] = permissions.Keys.ToArray(),
        };

        foreach (var (name, codes) in rolePermissions)
        {
            var role = await roleManager.FindByNameAsync(name);
            if (role is null)
            {
                var createResult = await roleManager.CreateAsync(new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"{name} permissions",
                });
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create the '{name}' role.");
                }

                role = await roleManager.FindByNameAsync(name)
                    ?? throw new InvalidOperationException($"The '{name}' role could not be loaded after creation.");
            }

            foreach (var code in codes)
            {
                var permission = permissionEntities[code];
                var exists = await dbContext.RolePermissions.AnyAsync(
                    mapping => mapping.RoleId == role.Id && mapping.PermissionId == permission.Id,
                    cancellationToken);
                if (!exists)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                    });
                }
            }
        }

        var vivaCategories = new (string Slug, string Name)[]
        {
            ("personal-profile", "Personal profile"),
            ("academic-background", "Academic background"),
            ("university-and-subject", "University and subject"),
            ("bangladesh-affairs", "Bangladesh affairs"),
            ("constitution", "Constitution"),
            ("government-and-administration", "Government and administration"),
            ("international-affairs", "International affairs"),
            ("current-affairs", "Current affairs"),
            ("geography", "Geography"),
            ("economy", "Economy"),
            ("science-and-ict", "Science and ICT"),
            ("ethics", "Ethics"),
            ("public-administration", "Public administration"),
            ("cadre-specific-research", "Cadre-specific research"),
            ("professional-experience", "Professional experience"),
            ("situational-questions", "Situational questions"),
            ("communication-and-reasoning", "Communication and reasoning"),
        };
        for (var index = 0; index < vivaCategories.Length; index++)
        {
            var (slug, name) = vivaCategories[index];
            var exists = await dbContext.VivaCategories.AnyAsync(category => category.Slug == slug, cancellationToken);
            if (!exists)
            {
                dbContext.VivaCategories.Add(new VivaCategory
                {
                    Id = Guid.NewGuid(),
                    Slug = slug,
                    Name = name,
                    DisplayOrder = index + 1,
                    IsActive = true,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded baseline roles and permissions.");
    }
}

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

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded baseline roles and permissions.");
    }
}

using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid,
        IdentityUserClaim<Guid>,
        UserRoleMapping,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExamStage> ExamStages => Set<ExamStage>();
    public DbSet<SyllabusVersion> SyllabusVersions => Set<SyllabusVersion>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SyllabusSubject> SyllabusSubjects => Set<SyllabusSubject>();
    public DbSet<Paper> Papers => Set<Paper>();
    public DbSet<SyllabusItem> SyllabusItems => Set<SyllabusItem>();
    public DbSet<SyllabusSourceReference> SyllabusSourceReferences => Set<SyllabusSourceReference>();
    public DbSet<SyllabusVersionSource> SyllabusVersionSources => Set<SyllabusVersionSource>();
    public DbSet<SyllabusItemSource> SyllabusItemSources => Set<SyllabusItemSource>();
    public DbSet<TopicProgress> TopicProgress => Set<TopicProgress>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(user => user.UpdatedAt).HasColumnType("datetime(6)");
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(role => role.Description).HasMaxLength(300);
        });

        builder.Entity<UserRoleMapping>(entity =>
        {
            entity.Property(mapping => mapping.AssignedAt).HasColumnType("datetime(6)");
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Code).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(300).IsRequired();
            entity.HasIndex(permission => permission.Code).IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(mapping => new { mapping.RoleId, mapping.PermissionId });
            entity.HasOne<ApplicationRole>()
                .WithMany()
                .HasForeignKey(mapping => mapping.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mapping => mapping.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(mapping => mapping.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Action).HasMaxLength(100).IsRequired();
            entity.Property(log => log.EntityType).HasMaxLength(150);
            entity.Property(log => log.EntityId).HasMaxLength(100);
            entity.Property(log => log.CorrelationId).HasMaxLength(100);
            entity.Property(log => log.OccurredAt).HasColumnType("datetime(6)");
            entity.HasIndex(log => new { log.ActorId, log.OccurredAt });
        });

        builder.ApplyConfiguration(new ExamStageConfiguration());
        builder.ApplyConfiguration(new SyllabusVersionConfiguration());
        builder.ApplyConfiguration(new SubjectConfiguration());
        builder.ApplyConfiguration(new SyllabusSubjectConfiguration());
        builder.ApplyConfiguration(new PaperConfiguration());
        builder.ApplyConfiguration(new SyllabusItemConfiguration());
        builder.ApplyConfiguration(new SyllabusSourceReferenceConfiguration());
        builder.ApplyConfiguration(new SyllabusVersionSourceConfiguration());
        builder.ApplyConfiguration(new SyllabusItemSourceConfiguration());
        builder.ApplyConfiguration(new TopicProgressConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<SyllabusVersion>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

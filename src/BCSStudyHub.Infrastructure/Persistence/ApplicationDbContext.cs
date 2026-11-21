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
    public DbSet<McqQuestion> McqQuestions => Set<McqQuestion>();
    public DbSet<McqQuestionOption> McqQuestionOptions => Set<McqQuestionOption>();
    public DbSet<McqQuestionSourceReference> McqQuestionSourceReferences => Set<McqQuestionSourceReference>();
    public DbSet<McqQuestionTag> McqQuestionTags => Set<McqQuestionTag>();
    public DbSet<McqExamSet> McqExamSets => Set<McqExamSet>();
    public DbSet<McqAttempt> McqAttempts => Set<McqAttempt>();
    public DbSet<McqAttemptQuestion> McqAttemptQuestions => Set<McqAttemptQuestion>();
    public DbSet<McqAttemptOption> McqAttemptOptions => Set<McqAttemptOption>();
    public DbSet<McqAttemptAnswer> McqAttemptAnswers => Set<McqAttemptAnswer>();
    public DbSet<McqBookmark> McqBookmarks => Set<McqBookmark>();
    public DbSet<McqMistake> McqMistakes => Set<McqMistake>();
    public DbSet<WrittenQuestion> WrittenQuestions => Set<WrittenQuestion>();
    public DbSet<WrittenQuestionSourceReference> WrittenQuestionSourceReferences => Set<WrittenQuestionSourceReference>();
    public DbSet<WrittenQuestionResource> WrittenQuestionResources => Set<WrittenQuestionResource>();
    public DbSet<WrittenQuestionTag> WrittenQuestionTags => Set<WrittenQuestionTag>();
    public DbSet<WrittenModelAnswer> WrittenModelAnswers => Set<WrittenModelAnswer>();
    public DbSet<WrittenModelAnswerReference> WrittenModelAnswerReferences => Set<WrittenModelAnswerReference>();
    public DbSet<WrittenAnswer> WrittenAnswers => Set<WrittenAnswer>();
    public DbSet<WrittenAnswerRevision> WrittenAnswerRevisions => Set<WrittenAnswerRevision>();
    public DbSet<WrittenBookmark> WrittenBookmarks => Set<WrittenBookmark>();
    public DbSet<WrittenGoal> WrittenGoals => Set<WrittenGoal>();
    public DbSet<WrittenStudyNote> WrittenStudyNotes => Set<WrittenStudyNote>();
    public DbSet<VivaCategory> VivaCategories => Set<VivaCategory>();
    public DbSet<VivaQuestion> VivaQuestions => Set<VivaQuestion>();
    public DbSet<VivaQuestionReference> VivaQuestionReferences => Set<VivaQuestionReference>();
    public DbSet<VivaBookmark> VivaBookmarks => Set<VivaBookmark>();
    public DbSet<VivaQuestionHistory> VivaQuestionHistory => Set<VivaQuestionHistory>();
    public DbSet<VivaAnswerNote> VivaAnswerNotes => Set<VivaAnswerNote>();
    public DbSet<VivaAnswerNoteRevision> VivaAnswerNoteRevisions => Set<VivaAnswerNoteRevision>();
    public DbSet<VivaMockSession> VivaMockSessions => Set<VivaMockSession>();
    public DbSet<VivaMockSessionQuestion> VivaMockSessionQuestions => Set<VivaMockSessionQuestion>();
    public DbSet<VivaProfilePreparation> VivaProfilePreparations => Set<VivaProfilePreparation>();
    public DbSet<LibraryResource> LibraryResources => Set<LibraryResource>();
    public DbSet<LibraryBookDetails> LibraryBookDetails => Set<LibraryBookDetails>();
    public DbSet<LibraryFile> LibraryFiles => Set<LibraryFile>();
    public DbSet<LibraryBookChapter> LibraryBookChapters => Set<LibraryBookChapter>();
    public DbSet<LibraryChapterTopic> LibraryChapterTopics => Set<LibraryChapterTopic>();
    public DbSet<LibraryChapterResource> LibraryChapterResources => Set<LibraryChapterResource>();
    public DbSet<LibraryResourceSubject> LibraryResourceSubjects => Set<LibraryResourceSubject>();
    public DbSet<LibraryResourceTopic> LibraryResourceTopics => Set<LibraryResourceTopic>();
    public DbSet<LibraryResourceTag> LibraryResourceTags => Set<LibraryResourceTag>();
    public DbSet<LibraryCollection> LibraryCollections => Set<LibraryCollection>();
    public DbSet<LibraryCollectionItem> LibraryCollectionItems => Set<LibraryCollectionItem>();
    public DbSet<LibraryBookmark> LibraryBookmarks => Set<LibraryBookmark>();
    public DbSet<LibraryReadingProgress> LibraryReadingProgress => Set<LibraryReadingProgress>();
    public DbSet<LibraryPersonalNote> LibraryPersonalNotes => Set<LibraryPersonalNote>();
    public DbSet<CurrentAffairsEntry> CurrentAffairsEntries => Set<CurrentAffairsEntry>();
    public DbSet<CurrentAffairsEntryTag> CurrentAffairsEntryTags => Set<CurrentAffairsEntryTag>();
    public DbSet<CurrentAffairsEntrySubject> CurrentAffairsEntrySubjects => Set<CurrentAffairsEntrySubject>();
    public DbSet<CurrentAffairsEntryTopic> CurrentAffairsEntryTopics => Set<CurrentAffairsEntryTopic>();
    public DbSet<CurrentAffairsEntryQuestion> CurrentAffairsEntryQuestions => Set<CurrentAffairsEntryQuestion>();
    public DbSet<CurrentAffairsEntryReference> CurrentAffairsEntryReferences => Set<CurrentAffairsEntryReference>();
    public DbSet<CurrentAffairsEntryResource> CurrentAffairsEntryResources => Set<CurrentAffairsEntryResource>();
    public DbSet<CurrentAffairsBookmark> CurrentAffairsBookmarks => Set<CurrentAffairsBookmark>();
    public DbSet<CurrentAffairsStudyNote> CurrentAffairsStudyNotes => Set<CurrentAffairsStudyNote>();
    public DbSet<CurrentAffairsRevision> CurrentAffairsRevisions => Set<CurrentAffairsRevision>();
    public DbSet<OfficialSource> OfficialSources => Set<OfficialSource>();
    public DbSet<OfficialSourceTag> OfficialSourceTags => Set<OfficialSourceTag>();
    public DbSet<StudyGoal> StudyGoals => Set<StudyGoal>();
    public DbSet<StudySchedule> StudySchedules => Set<StudySchedule>();
    public DbSet<StudyTask> StudyTasks => Set<StudyTask>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<PlannerSettings> PlannerSettings => Set<PlannerSettings>();
    public DbSet<RevisionItem> RevisionItems => Set<RevisionItem>();
    public DbSet<RevisionLog> RevisionLogs => Set<RevisionLog>();
    public DbSet<MistakeEntry> MistakeEntries => Set<MistakeEntry>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

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
        builder.ApplyConfiguration(new McqQuestionConfiguration());
        builder.ApplyConfiguration(new McqQuestionOptionConfiguration());
        builder.ApplyConfiguration(new McqQuestionSourceReferenceConfiguration());
        builder.ApplyConfiguration(new McqQuestionTagConfiguration());
        builder.ApplyConfiguration(new McqExamSetConfiguration());
        builder.ApplyConfiguration(new McqAttemptConfiguration());
        builder.ApplyConfiguration(new McqAttemptQuestionConfiguration());
        builder.ApplyConfiguration(new McqAttemptOptionConfiguration());
        builder.ApplyConfiguration(new McqAttemptAnswerConfiguration());
        builder.ApplyConfiguration(new McqBookmarkConfiguration());
        builder.ApplyConfiguration(new McqMistakeConfiguration());
        builder.ApplyConfiguration(new WrittenQuestionConfiguration());
        builder.ApplyConfiguration(new WrittenQuestionSourceReferenceConfiguration());
        builder.ApplyConfiguration(new WrittenQuestionResourceConfiguration());
        builder.ApplyConfiguration(new WrittenQuestionTagConfiguration());
        builder.ApplyConfiguration(new WrittenModelAnswerConfiguration());
        builder.ApplyConfiguration(new WrittenModelAnswerReferenceConfiguration());
        builder.ApplyConfiguration(new WrittenAnswerConfiguration());
        builder.ApplyConfiguration(new WrittenAnswerRevisionConfiguration());
        builder.ApplyConfiguration(new WrittenBookmarkConfiguration());
        builder.ApplyConfiguration(new WrittenGoalConfiguration());
        builder.ApplyConfiguration(new WrittenStudyNoteConfiguration());
        builder.ApplyConfiguration(new VivaCategoryConfiguration());
        builder.ApplyConfiguration(new VivaQuestionConfiguration());
        builder.ApplyConfiguration(new VivaQuestionReferenceConfiguration());
        builder.ApplyConfiguration(new VivaBookmarkConfiguration());
        builder.ApplyConfiguration(new VivaQuestionHistoryConfiguration());
        builder.ApplyConfiguration(new VivaAnswerNoteConfiguration());
        builder.ApplyConfiguration(new VivaAnswerNoteRevisionConfiguration());
        builder.ApplyConfiguration(new VivaMockSessionConfiguration());
        builder.ApplyConfiguration(new VivaMockSessionQuestionConfiguration());
        builder.ApplyConfiguration(new VivaProfilePreparationConfiguration());
        builder.ApplyConfiguration(new LibraryResourceConfiguration());
        builder.ApplyConfiguration(new LibraryBookDetailsConfiguration());
        builder.ApplyConfiguration(new LibraryFileConfiguration());
        builder.ApplyConfiguration(new LibraryBookChapterConfiguration());
        builder.ApplyConfiguration(new LibraryChapterTopicConfiguration());
        builder.ApplyConfiguration(new LibraryChapterResourceConfiguration());
        builder.ApplyConfiguration(new LibraryResourceSubjectConfiguration());
        builder.ApplyConfiguration(new LibraryResourceTopicConfiguration());
        builder.ApplyConfiguration(new LibraryResourceTagConfiguration());
        builder.ApplyConfiguration(new LibraryCollectionConfiguration());
        builder.ApplyConfiguration(new LibraryCollectionItemConfiguration());
        builder.ApplyConfiguration(new LibraryBookmarkConfiguration());
        builder.ApplyConfiguration(new LibraryReadingProgressConfiguration());
        builder.ApplyConfiguration(new LibraryPersonalNoteConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntryConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntryTagConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntrySubjectConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntryTopicConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntryQuestionConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntryReferenceConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsEntryResourceConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsBookmarkConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsStudyNoteConfiguration());
        builder.ApplyConfiguration(new CurrentAffairsRevisionConfiguration());
        builder.ApplyConfiguration(new OfficialSourceConfiguration());
        builder.ApplyConfiguration(new OfficialSourceTagConfiguration());
        builder.ApplyConfiguration(new StudyGoalConfiguration());
        builder.ApplyConfiguration(new StudyScheduleConfiguration());
        builder.ApplyConfiguration(new StudyTaskConfiguration());
        builder.ApplyConfiguration(new StudySessionConfiguration());
        builder.ApplyConfiguration(new PlannerSettingsConfiguration());
        builder.ApplyConfiguration(new RevisionItemConfiguration());
        builder.ApplyConfiguration(new RevisionLogConfiguration());
        builder.ApplyConfiguration(new MistakeEntryConfiguration());
        builder.Entity<SiteSettings>(entity =>
        {
            entity.ToTable("SiteSettings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
            entity.Property(settings => settings.SiteName).HasMaxLength(80).IsRequired();
            entity.Property(settings => settings.Tagline).HasMaxLength(200);
            entity.Property(settings => settings.AnnouncementText).HasMaxLength(500);
            entity.Property(settings => settings.AnnouncementTone).HasConversion<string>().HasMaxLength(16);
            entity.Property(settings => settings.AnnouncementStartsAt).HasColumnType("datetime(6)");
            entity.Property(settings => settings.AnnouncementEndsAt).HasColumnType("datetime(6)");
            entity.Property(settings => settings.SupportEmail).HasMaxLength(254);
            entity.Property(settings => settings.FooterNotice).HasMaxLength(500);
            entity.Property(settings => settings.UpdatedAt).HasColumnType("datetime(6)");
        });
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

        foreach (var entry in ChangeTracker.Entries<McqQuestion>())
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

        foreach (var entry in ChangeTracker.Entries<McqExamSet>())
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

        foreach (var entry in ChangeTracker.Entries<WrittenQuestion>())
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

        foreach (var entry in ChangeTracker.Entries<WrittenGoal>())
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

        foreach (var entry in ChangeTracker.Entries<WrittenStudyNote>())
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

        foreach (var entry in ChangeTracker.Entries<VivaCategory>())
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

        foreach (var entry in ChangeTracker.Entries<VivaQuestion>())
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

        foreach (var entry in ChangeTracker.Entries<VivaProfilePreparation>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<LibraryResource>())
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

        foreach (var entry in ChangeTracker.Entries<LibraryCollection>())
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

        foreach (var entry in ChangeTracker.Entries<LibraryPersonalNote>())
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

        foreach (var entry in ChangeTracker.Entries<CurrentAffairsEntry>())
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

        foreach (var entry in ChangeTracker.Entries<OfficialSource>())
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

        foreach (var entry in ChangeTracker.Entries<CurrentAffairsStudyNote>())
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

        StampCreatedUpdated(ChangeTracker.Entries<StudyGoal>(), goal => goal.CreatedAt = now, goal => goal.UpdatedAt = now);
        StampCreatedUpdated(ChangeTracker.Entries<StudySchedule>(), schedule => schedule.CreatedAt = now, schedule => schedule.UpdatedAt = now);
        StampCreatedUpdated(ChangeTracker.Entries<StudyTask>(), task => task.CreatedAt = now, task => task.UpdatedAt = now);
        StampCreatedUpdated(ChangeTracker.Entries<MistakeEntry>(), mistake => mistake.CreatedAt = now, mistake => mistake.UpdatedAt = now);

        foreach (var entry in ChangeTracker.Entries<LibraryReadingProgress>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void StampCreatedUpdated<T>(
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>> entries,
        Action<T> setCreated,
        Action<T> setUpdated)
        where T : class
    {
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                setCreated(entry.Entity);
                setUpdated(entry.Entity);
            }
            else if (entry.State == EntityState.Modified)
            {
                setUpdated(entry.Entity);
            }
        }
    }
}

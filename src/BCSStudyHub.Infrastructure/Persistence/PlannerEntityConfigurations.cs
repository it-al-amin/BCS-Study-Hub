using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class StudyGoalConfiguration : IEntityTypeConfiguration<StudyGoal>
{
    public void Configure(EntityTypeBuilder<StudyGoal> builder)
    {
        builder.ToTable("StudyGoals");
        builder.HasKey(goal => goal.Id);
        builder.Property(goal => goal.Period).HasConversion<string>().HasMaxLength(16);
        builder.Property(goal => goal.PeriodStart).HasColumnType("date");
        builder.Property(goal => goal.Title).HasMaxLength(200).IsRequired();
        builder.Property(goal => goal.Metric).HasConversion<string>().HasMaxLength(32);
        builder.Property(goal => goal.Stage).HasConversion<string>().HasMaxLength(16);
        builder.Property(goal => goal.Notes).HasMaxLength(1000);
        builder.Property(goal => goal.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(goal => goal.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(goal => new { goal.UserId, goal.Period, goal.PeriodStart });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(goal => goal.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(goal => goal.Subject).WithMany().HasForeignKey(goal => goal.SubjectId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class StudyScheduleConfiguration : IEntityTypeConfiguration<StudySchedule>
{
    public void Configure(EntityTypeBuilder<StudySchedule> builder)
    {
        builder.ToTable("StudySchedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Title).HasMaxLength(200).IsRequired();
        builder.Property(schedule => schedule.StartTime).HasColumnType("time(6)");
        builder.Property(schedule => schedule.Priority).HasConversion<string>().HasMaxLength(16);
        builder.Property(schedule => schedule.Stage).HasConversion<string>().HasMaxLength(16);
        builder.Property(schedule => schedule.StartDate).HasColumnType("date");
        builder.Property(schedule => schedule.EndDate).HasColumnType("date");
        builder.Property(schedule => schedule.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(schedule => schedule.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(schedule => new { schedule.UserId, schedule.IsActive });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(schedule => schedule.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(schedule => schedule.Subject).WithMany().HasForeignKey(schedule => schedule.SubjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(schedule => schedule.SyllabusItem).WithMany().HasForeignKey(schedule => schedule.SyllabusItemId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class StudyTaskConfiguration : IEntityTypeConfiguration<StudyTask>
{
    public void Configure(EntityTypeBuilder<StudyTask> builder)
    {
        builder.ToTable("StudyTasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Title).HasMaxLength(200).IsRequired();
        builder.Property(task => task.Description).HasMaxLength(4000);
        builder.Property(task => task.Priority).HasConversion<string>().HasMaxLength(16);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(task => task.ScheduledDate).HasColumnType("date");
        builder.Property(task => task.StartTime).HasColumnType("time(6)");
        builder.Property(task => task.Stage).HasConversion<string>().HasMaxLength(16);
        builder.Property(task => task.CompletedAt).HasColumnType("datetime(6)");
        builder.Property(task => task.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(task => task.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(task => new { task.UserId, task.ScheduledDate });
        builder.HasIndex(task => new { task.UserId, task.Status });
        builder.HasIndex(task => new { task.ScheduleId, task.ScheduledDate }).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(task => task.Subject).WithMany().HasForeignKey(task => task.SubjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(task => task.SyllabusItem).WithMany().HasForeignKey(task => task.SyllabusItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(task => task.Schedule).WithMany().HasForeignKey(task => task.ScheduleId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable("StudySessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.StartedAt).HasColumnType("datetime(6)");
        builder.Property(session => session.EndedAt).HasColumnType("datetime(6)");
        builder.Property(session => session.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(session => session.Activity).HasConversion<string>().HasMaxLength(16);
        builder.Property(session => session.Stage).HasConversion<string>().HasMaxLength(16);
        builder.Property(session => session.Notes).HasMaxLength(2000);
        builder.HasIndex(session => new { session.UserId, session.StartedAt });
        builder.HasIndex(session => new { session.UserId, session.EndedAt });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(session => session.Task).WithMany().HasForeignKey(session => session.TaskId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(session => session.Subject).WithMany().HasForeignKey(session => session.SubjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(session => session.SyllabusItem).WithMany().HasForeignKey(session => session.SyllabusItemId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class PlannerSettingsConfiguration : IEntityTypeConfiguration<PlannerSettings>
{
    public void Configure(EntityTypeBuilder<PlannerSettings> builder)
    {
        builder.ToTable("PlannerSettings");
        builder.HasKey(settings => settings.UserId);
        builder.Property(settings => settings.RevisionIntervalsJson).HasMaxLength(400).IsRequired();
        builder.Property(settings => settings.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<PlannerSettings>(settings => settings.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RevisionItemConfiguration : IEntityTypeConfiguration<RevisionItem>
{
    public void Configure(EntityTypeBuilder<RevisionItem> builder)
    {
        builder.ToTable("RevisionItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SourceType).HasConversion<string>().HasMaxLength(24);
        builder.Property(item => item.Reason).HasConversion<string>().HasMaxLength(16);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(item => item.LastOutcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.DueOn).HasColumnType("date");
        builder.Property(item => item.LastReviewedAt).HasColumnType("datetime(6)");
        builder.Property(item => item.CreatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(item => new { item.UserId, item.Status, item.DueOn });
        builder.HasIndex(item => new { item.UserId, item.SourceType, item.SourceId });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.SyllabusItem).WithMany().HasForeignKey(item => item.SyllabusItemId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class RevisionLogConfiguration : IEntityTypeConfiguration<RevisionLog>
{
    public void Configure(EntityTypeBuilder<RevisionLog> builder)
    {
        builder.ToTable("RevisionLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.SourceType).HasConversion<string>().HasMaxLength(24);
        builder.Property(log => log.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(log => log.Title).HasMaxLength(300).IsRequired();
        builder.Property(log => log.ReviewedAt).HasColumnType("datetime(6)");
        builder.HasIndex(log => new { log.UserId, log.ReviewedAt });
        builder.HasIndex(log => new { log.UserId, log.SourceType, log.SourceId });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(log => log.RevisionItem).WithMany().HasForeignKey(log => log.RevisionItemId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class MistakeEntryConfiguration : IEntityTypeConfiguration<MistakeEntry>
{
    public void Configure(EntityTypeBuilder<MistakeEntry> builder)
    {
        builder.ToTable("MistakeEntries");
        builder.HasKey(mistake => mistake.Id);
        builder.Property(mistake => mistake.SourceType).HasConversion<string>().HasMaxLength(16);
        builder.Property(mistake => mistake.Category).HasConversion<string>().HasMaxLength(16);
        builder.Property(mistake => mistake.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(mistake => mistake.QuestionText).HasMaxLength(8000).IsRequired();
        builder.Property(mistake => mistake.YourAnswer).HasMaxLength(4000);
        builder.Property(mistake => mistake.CorrectAnswerReference).HasMaxLength(4000).IsRequired();
        builder.Property(mistake => mistake.Explanation).HasMaxLength(8000);
        builder.Property(mistake => mistake.ReviewOn).HasColumnType("date");
        builder.Property(mistake => mistake.ResolvedAt).HasColumnType("datetime(6)");
        builder.Property(mistake => mistake.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(mistake => mistake.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(mistake => new { mistake.UserId, mistake.Status, mistake.UpdatedAt });
        builder.HasIndex(mistake => new { mistake.UserId, mistake.SourceType, mistake.QuestionId });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(mistake => mistake.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mistake => mistake.SyllabusItem).WithMany().HasForeignKey(mistake => mistake.SyllabusItemId).OnDelete(DeleteBehavior.SetNull);
    }
}

using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class ExamStageConfiguration : IEntityTypeConfiguration<ExamStage>
{
    public void Configure(EntityTypeBuilder<ExamStage> builder)
    {
        builder.ToTable("ExamStages");
        builder.HasKey(stage => stage.Id);
        builder.Property(stage => stage.Code).HasMaxLength(40).IsRequired();
        builder.Property(stage => stage.Name).HasMaxLength(120).IsRequired();
        builder.Property(stage => stage.LocalizedName).HasMaxLength(160).IsRequired();
        builder.HasIndex(stage => stage.Code).IsUnique();
    }
}

internal sealed class SyllabusVersionConfiguration : IEntityTypeConfiguration<SyllabusVersion>
{
    public void Configure(EntityTypeBuilder<SyllabusVersion> builder)
    {
        builder.ToTable("SyllabusVersions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.VersionLabel).HasMaxLength(120).IsRequired();
        builder.Property(version => version.TrackCode).HasMaxLength(40).IsRequired();
        builder.Property(version => version.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(version => version.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(version => version.Coverage).HasConversion<string>().HasMaxLength(20);
        builder.Property(version => version.EffectiveFrom).HasColumnType("date");
        builder.Property(version => version.EffectiveTo).HasColumnType("date");
        builder.Property(version => version.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(version => version.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(version => version.PublishedAt).HasColumnType("datetime(6)");
        builder.HasIndex(version => new { version.ExamStageId, version.TrackCode, version.VersionLabel }).IsUnique();
        builder.HasOne(version => version.ExamStage)
            .WithMany(stage => stage.SyllabusVersions)
            .HasForeignKey(version => version.ExamStageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasKey(subject => subject.Id);
        builder.Property(subject => subject.Code).HasMaxLength(60).IsRequired();
        builder.Property(subject => subject.Name).HasMaxLength(240).IsRequired();
        builder.Property(subject => subject.Description).HasMaxLength(2000);
        builder.HasIndex(subject => subject.Code).IsUnique();
    }
}

internal sealed class SyllabusSubjectConfiguration : IEntityTypeConfiguration<SyllabusSubject>
{
    public void Configure(EntityTypeBuilder<SyllabusSubject> builder)
    {
        builder.ToTable("SyllabusSubjects");
        builder.HasKey(mapping => mapping.Id);
        builder.HasIndex(mapping => new { mapping.SyllabusVersionId, mapping.SubjectId }).IsUnique();
        builder.HasIndex(mapping => new { mapping.SyllabusVersionId, mapping.SortOrder });
        builder.HasOne(mapping => mapping.SyllabusVersion)
            .WithMany(version => version.Subjects)
            .HasForeignKey(mapping => mapping.SyllabusVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.Subject)
            .WithMany(subject => subject.SyllabusVersions)
            .HasForeignKey(mapping => mapping.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaperConfiguration : IEntityTypeConfiguration<Paper>
{
    public void Configure(EntityTypeBuilder<Paper> builder)
    {
        builder.ToTable("Papers");
        builder.HasKey(paper => paper.Id);
        builder.Property(paper => paper.Code).HasMaxLength(60).IsRequired();
        builder.Property(paper => paper.Name).HasMaxLength(240).IsRequired();
        builder.HasIndex(paper => new { paper.SyllabusSubjectId, paper.Code }).IsUnique();
        builder.HasOne(paper => paper.SyllabusSubject)
            .WithMany(mapping => mapping.Papers)
            .HasForeignKey(paper => paper.SyllabusSubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SyllabusItemConfiguration : IEntityTypeConfiguration<SyllabusItem>
{
    public void Configure(EntityTypeBuilder<SyllabusItem> builder)
    {
        builder.ToTable("SyllabusItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(item => new { item.SyllabusSubjectId, item.Kind, item.SortOrder });
        builder.HasIndex(item => item.ParentId);
        builder.HasOne(item => item.SyllabusSubject)
            .WithMany(mapping => mapping.Items)
            .HasForeignKey(item => item.SyllabusSubjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Paper)
            .WithMany(paper => paper.Items)
            .HasForeignKey(item => item.PaperId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Parent)
            .WithMany(item => item.Children)
            .HasForeignKey(item => item.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SyllabusSourceReferenceConfiguration : IEntityTypeConfiguration<SyllabusSourceReference>
{
    public void Configure(EntityTypeBuilder<SyllabusSourceReference> builder)
    {
        builder.ToTable("SyllabusSourceReferences");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.Title).HasMaxLength(300).IsRequired();
        builder.Property(source => source.Publisher).HasMaxLength(180).IsRequired();
        builder.Property(source => source.Url).HasMaxLength(512).IsRequired();
        builder.Property(source => source.SourceType).HasMaxLength(40).IsRequired();
        builder.Property(source => source.Notes).HasMaxLength(2000);
        builder.Property(source => source.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(source => source.PublishedOn).HasColumnType("date");
        builder.Property(source => source.LastVerifiedOn).HasColumnType("date");
        builder.HasIndex(source => source.Url).IsUnique();
    }
}

internal sealed class SyllabusVersionSourceConfiguration : IEntityTypeConfiguration<SyllabusVersionSource>
{
    public void Configure(EntityTypeBuilder<SyllabusVersionSource> builder)
    {
        builder.ToTable("SyllabusVersionSources");
        builder.HasKey(mapping => new { mapping.SyllabusVersionId, mapping.SourceReferenceId });
        builder.Property(mapping => mapping.Notes).HasMaxLength(1000);
        builder.HasOne(mapping => mapping.SyllabusVersion)
            .WithMany(version => version.SourceReferences)
            .HasForeignKey(mapping => mapping.SyllabusVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.SourceReference)
            .WithMany(source => source.SyllabusVersions)
            .HasForeignKey(mapping => mapping.SourceReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SyllabusItemSourceConfiguration : IEntityTypeConfiguration<SyllabusItemSource>
{
    public void Configure(EntityTypeBuilder<SyllabusItemSource> builder)
    {
        builder.ToTable("SyllabusItemSources");
        builder.HasKey(mapping => new { mapping.SyllabusItemId, mapping.SourceReferenceId });
        builder.HasOne(mapping => mapping.SyllabusItem)
            .WithMany(item => item.SourceReferences)
            .HasForeignKey(mapping => mapping.SyllabusItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.SourceReference)
            .WithMany(source => source.SyllabusItems)
            .HasForeignKey(mapping => mapping.SourceReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TopicProgressConfiguration : IEntityTypeConfiguration<TopicProgress>
{
    public void Configure(EntityTypeBuilder<TopicProgress> builder)
    {
        builder.ToTable("TopicProgress");
        builder.HasKey(progress => progress.Id);
        builder.Property(progress => progress.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(progress => progress.CompletedAt).HasColumnType("datetime(6)");
        builder.HasIndex(progress => new { progress.UserId, progress.SyllabusItemId }).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(progress => progress.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(progress => progress.SyllabusItem)
            .WithMany(item => item.ProgressRecords)
            .HasForeignKey(progress => progress.SyllabusItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class WrittenQuestionConfiguration : IEntityTypeConfiguration<WrittenQuestion>
{
    public void Configure(EntityTypeBuilder<WrittenQuestion> builder)
    {
        builder.ToTable("WrittenQuestions");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.Prompt).HasMaxLength(12000).IsRequired();
        builder.Property(question => question.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(question => question.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(question => question.ExamReference).HasMaxLength(160);
        builder.Property(question => question.Marks).HasPrecision(8, 2);
        builder.Property(question => question.AnswerGuidance).HasMaxLength(8000);
        builder.Property(question => question.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(question => question.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(question => new { question.Status, question.Type });
        builder.HasIndex(question => question.BcsExamYear);
        builder.HasIndex(question => question.SyllabusItemId);
        builder.HasOne(question => question.SyllabusItem)
            .WithMany()
            .HasForeignKey(question => question.SyllabusItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(question => question.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WrittenQuestionSourceReferenceConfiguration : IEntityTypeConfiguration<WrittenQuestionSourceReference>
{
    public void Configure(EntityTypeBuilder<WrittenQuestionSourceReference> builder)
    {
        builder.ToTable("WrittenQuestionSourceReferences");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.Title).HasMaxLength(300).IsRequired();
        builder.Property(source => source.Publisher).HasMaxLength(180).IsRequired();
        builder.Property(source => source.Url).HasMaxLength(512).IsRequired();
        builder.Property(source => source.Citation).HasMaxLength(500);
        builder.Property(source => source.PublishedOn).HasColumnType("date");
        builder.Property(source => source.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(source => source.QuestionId);
        builder.HasOne(source => source.Question)
            .WithMany(question => question.Sources)
            .HasForeignKey(source => source.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WrittenQuestionResourceConfiguration : IEntityTypeConfiguration<WrittenQuestionResource>
{
    public void Configure(EntityTypeBuilder<WrittenQuestionResource> builder)
    {
        builder.ToTable("WrittenQuestionResources");
        builder.HasKey(resource => resource.Id);
        builder.Property(resource => resource.Title).HasMaxLength(300).IsRequired();
        builder.Property(resource => resource.Url).HasMaxLength(512).IsRequired();
        builder.HasIndex(resource => new { resource.QuestionId, resource.Url }).IsUnique();
        builder.HasOne(resource => resource.Question)
            .WithMany(question => question.Resources)
            .HasForeignKey(resource => resource.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WrittenQuestionTagConfiguration : IEntityTypeConfiguration<WrittenQuestionTag>
{
    public void Configure(EntityTypeBuilder<WrittenQuestionTag> builder)
    {
        builder.ToTable("WrittenQuestionTags");
        builder.HasKey(tag => new { tag.QuestionId, tag.Tag });
        builder.Property(tag => tag.Tag).HasMaxLength(60);
        builder.HasIndex(tag => tag.Tag);
        builder.HasOne(tag => tag.Question)
            .WithMany(question => question.Tags)
            .HasForeignKey(tag => tag.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WrittenModelAnswerConfiguration : IEntityTypeConfiguration<WrittenModelAnswer>
{
    public void Configure(EntityTypeBuilder<WrittenModelAnswer> builder)
    {
        builder.ToTable("WrittenModelAnswers");
        builder.HasKey(answer => answer.Id);
        builder.Property(answer => answer.Content).HasColumnType("longtext").IsRequired();
        builder.Property(answer => answer.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(answer => answer.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(answer => answer.QuestionId).IsUnique();
        builder.HasOne(answer => answer.Question)
            .WithOne(question => question.ModelAnswer)
            .HasForeignKey<WrittenModelAnswer>(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(answer => answer.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WrittenModelAnswerReferenceConfiguration : IEntityTypeConfiguration<WrittenModelAnswerReference>
{
    public void Configure(EntityTypeBuilder<WrittenModelAnswerReference> builder)
    {
        builder.ToTable("WrittenModelAnswerReferences");
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Title).HasMaxLength(300).IsRequired();
        builder.Property(reference => reference.Publisher).HasMaxLength(180).IsRequired();
        builder.Property(reference => reference.Url).HasMaxLength(512).IsRequired();
        builder.Property(reference => reference.Citation).HasMaxLength(500);
        builder.Property(reference => reference.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(reference => reference.ModelAnswerId);
        builder.HasOne(reference => reference.ModelAnswer)
            .WithMany(answer => answer.References)
            .HasForeignKey(reference => reference.ModelAnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WrittenAnswerConfiguration : IEntityTypeConfiguration<WrittenAnswer>
{
    public void Configure(EntityTypeBuilder<WrittenAnswer> builder)
    {
        builder.ToTable("WrittenAnswers");
        builder.HasKey(answer => answer.Id);
        builder.Property(answer => answer.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(answer => answer.Content).HasColumnType("longtext").IsRequired();
        builder.Property(answer => answer.SelfAssessmentJson).HasColumnType("longtext");
        builder.Property(answer => answer.StartedAt).HasColumnType("datetime(6)");
        builder.Property(answer => answer.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(answer => answer.SubmittedAt).HasColumnType("datetime(6)");
        builder.HasIndex(answer => new { answer.UserId, answer.UpdatedAt });
        builder.HasIndex(answer => new { answer.UserId, answer.QuestionId, answer.Status });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(answer => answer.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(answer => answer.Question)
            .WithMany()
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WrittenAnswerRevisionConfiguration : IEntityTypeConfiguration<WrittenAnswerRevision>
{
    public void Configure(EntityTypeBuilder<WrittenAnswerRevision> builder)
    {
        builder.ToTable("WrittenAnswerRevisions");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Kind).HasConversion<string>().HasMaxLength(24);
        builder.Property(revision => revision.Content).HasColumnType("longtext").IsRequired();
        builder.Property(revision => revision.SelfAssessmentJson).HasColumnType("longtext");
        builder.Property(revision => revision.CreatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(revision => new { revision.AnswerId, revision.RevisionNumber }).IsUnique();
        builder.HasOne(revision => revision.Answer)
            .WithMany(answer => answer.Revisions)
            .HasForeignKey(revision => revision.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WrittenBookmarkConfiguration : IEntityTypeConfiguration<WrittenBookmark>
{
    public void Configure(EntityTypeBuilder<WrittenBookmark> builder)
    {
        builder.ToTable("WrittenBookmarks");
        builder.HasKey(bookmark => new { bookmark.UserId, bookmark.QuestionId });
        builder.Property(bookmark => bookmark.CreatedAt).HasColumnType("datetime(6)");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(bookmark => bookmark.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bookmark => bookmark.Question)
            .WithMany()
            .HasForeignKey(bookmark => bookmark.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WrittenGoalConfiguration : IEntityTypeConfiguration<WrittenGoal>
{
    public void Configure(EntityTypeBuilder<WrittenGoal> builder)
    {
        builder.ToTable("WrittenGoals");
        builder.HasKey(goal => goal.Id);
        builder.Property(goal => goal.StartDate).HasColumnType("date");
        builder.Property(goal => goal.EndDate).HasColumnType("date");
        builder.Property(goal => goal.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(goal => goal.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(goal => new { goal.UserId, goal.StartDate, goal.EndDate });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(goal => goal.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WrittenStudyNoteConfiguration : IEntityTypeConfiguration<WrittenStudyNote>
{
    public void Configure(EntityTypeBuilder<WrittenStudyNote> builder)
    {
        builder.ToTable("WrittenStudyNotes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Title).HasMaxLength(200).IsRequired();
        builder.Property(note => note.Content).HasColumnType("longtext").IsRequired();
        builder.Property(note => note.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(note => note.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(note => new { note.UserId, note.SyllabusItemId, note.UpdatedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(note => note.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(note => note.SyllabusItem)
            .WithMany()
            .HasForeignKey(note => note.SyllabusItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class McqQuestionConfiguration : IEntityTypeConfiguration<McqQuestion>
{
    public void Configure(EntityTypeBuilder<McqQuestion> builder)
    {
        builder.ToTable("McqQuestions");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.Prompt).HasMaxLength(5000).IsRequired();
        builder.Property(question => question.Explanation).HasMaxLength(8000);
        builder.Property(question => question.ImageUrl).HasMaxLength(1024);
        builder.Property(question => question.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(question => question.Difficulty).HasConversion<string>().HasMaxLength(20);
        builder.Property(question => question.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(question => question.ExamReference).HasMaxLength(160);
        builder.Property(question => question.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(question => question.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(question => new { question.Status, question.Difficulty });
        builder.HasIndex(question => question.BcsExamYear);
        builder.HasIndex(question => question.ExamNumber);
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

internal sealed class McqQuestionOptionConfiguration : IEntityTypeConfiguration<McqQuestionOption>
{
    public void Configure(EntityTypeBuilder<McqQuestionOption> builder)
    {
        builder.ToTable("McqQuestionOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Text).HasMaxLength(2000).IsRequired();
        builder.HasIndex(option => new { option.QuestionId, option.SortOrder }).IsUnique();
        builder.HasOne(option => option.Question)
            .WithMany(question => question.Options)
            .HasForeignKey(option => option.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class McqQuestionSourceReferenceConfiguration : IEntityTypeConfiguration<McqQuestionSourceReference>
{
    public void Configure(EntityTypeBuilder<McqQuestionSourceReference> builder)
    {
        builder.ToTable("McqQuestionSourceReferences");
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

internal sealed class McqQuestionTagConfiguration : IEntityTypeConfiguration<McqQuestionTag>
{
    public void Configure(EntityTypeBuilder<McqQuestionTag> builder)
    {
        builder.ToTable("McqQuestionTags");
        builder.HasKey(tag => new { tag.QuestionId, tag.Tag });
        builder.Property(tag => tag.Tag).HasMaxLength(60);
        builder.HasIndex(tag => tag.Tag);
        builder.HasOne(tag => tag.Question)
            .WithMany(question => question.Tags)
            .HasForeignKey(tag => tag.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class McqExamSetConfiguration : IEntityTypeConfiguration<McqExamSet>
{
    public void Configure(EntityTypeBuilder<McqExamSet> builder)
    {
        builder.ToTable("McqExamSets");
        builder.HasKey(set => set.Id);
        builder.Property(set => set.Name).HasMaxLength(180).IsRequired();
        builder.Property(set => set.Instructions).HasMaxLength(3000).IsRequired();
        builder.Property(set => set.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(set => set.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(set => set.Difficulty).HasConversion<string>().HasMaxLength(20);
        builder.Property(set => set.MarksCorrect).HasPrecision(8, 2);
        builder.Property(set => set.MarksIncorrect).HasPrecision(8, 2);
        builder.Property(set => set.MarksUnanswered).HasPrecision(8, 2);
        builder.Property(set => set.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(set => set.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(set => new { set.Status, set.Mode });
        builder.HasOne(set => set.SyllabusVersion)
            .WithMany()
            .HasForeignKey(set => set.SyllabusVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(set => set.Subject)
            .WithMany()
            .HasForeignKey(set => set.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(set => set.SyllabusItem)
            .WithMany()
            .HasForeignKey(set => set.SyllabusItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(set => set.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class McqAttemptConfiguration : IEntityTypeConfiguration<McqAttempt>
{
    public void Configure(EntityTypeBuilder<McqAttempt> builder)
    {
        builder.ToTable("McqAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.ExamSetNameSnapshot).HasMaxLength(180).IsRequired();
        builder.Property(attempt => attempt.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(attempt => attempt.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(attempt => attempt.Score).HasPrecision(10, 2);
        builder.Property(attempt => attempt.MaximumScore).HasPrecision(10, 2);
        builder.Property(attempt => attempt.MarksCorrect).HasPrecision(8, 2);
        builder.Property(attempt => attempt.MarksIncorrect).HasPrecision(8, 2);
        builder.Property(attempt => attempt.MarksUnanswered).HasPrecision(8, 2);
        builder.Property(attempt => attempt.StartedAt).HasColumnType("datetime(6)");
        builder.Property(attempt => attempt.ExpiresAt).HasColumnType("datetime(6)");
        builder.Property(attempt => attempt.SubmittedAt).HasColumnType("datetime(6)");
        builder.HasIndex(attempt => new { attempt.UserId, attempt.StartedAt });
        builder.HasIndex(attempt => new { attempt.UserId, attempt.Status });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(attempt => attempt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(attempt => attempt.ExamSet)
            .WithMany()
            .HasForeignKey(attempt => attempt.ExamSetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class McqAttemptQuestionConfiguration : IEntityTypeConfiguration<McqAttemptQuestion>
{
    public void Configure(EntityTypeBuilder<McqAttemptQuestion> builder)
    {
        builder.ToTable("McqAttemptQuestions");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.PromptSnapshot).HasMaxLength(5000).IsRequired();
        builder.Property(question => question.ExplanationSnapshot).HasMaxLength(8000);
        builder.Property(question => question.ImageUrlSnapshot).HasMaxLength(1024);
        builder.Property(question => question.TopicTitleSnapshot).HasMaxLength(300).IsRequired();
        builder.Property(question => question.ParentTopicTitleSnapshot).HasMaxLength(300);
        builder.Property(question => question.SourcesSnapshotJson).HasColumnType("longtext").IsRequired();
        builder.Property(question => question.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(question => question.Difficulty).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(question => new { question.AttemptId, question.Position }).IsUnique();
        builder.HasIndex(question => new { question.AttemptId, question.QuestionId }).IsUnique();
        builder.HasOne(question => question.Attempt)
            .WithMany(attempt => attempt.Questions)
            .HasForeignKey(question => question.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class McqAttemptOptionConfiguration : IEntityTypeConfiguration<McqAttemptOption>
{
    public void Configure(EntityTypeBuilder<McqAttemptOption> builder)
    {
        builder.ToTable("McqAttemptOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.TextSnapshot).HasMaxLength(2000).IsRequired();
        builder.HasIndex(option => new { option.AttemptQuestionId, option.SortOrder }).IsUnique();
        builder.HasOne(option => option.AttemptQuestion)
            .WithMany(question => question.Options)
            .HasForeignKey(option => option.AttemptQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class McqAttemptAnswerConfiguration : IEntityTypeConfiguration<McqAttemptAnswer>
{
    public void Configure(EntityTypeBuilder<McqAttemptAnswer> builder)
    {
        builder.ToTable("McqAttemptAnswers");
        builder.HasKey(answer => answer.AttemptQuestionId);
        builder.Property(answer => answer.SelectedOptionIdsJson).HasColumnType("longtext").IsRequired();
        builder.Property(answer => answer.MarksAwarded).HasPrecision(8, 2);
        builder.Property(answer => answer.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasOne(answer => answer.AttemptQuestion)
            .WithOne(question => question.Answer)
            .HasForeignKey<McqAttemptAnswer>(answer => answer.AttemptQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class McqBookmarkConfiguration : IEntityTypeConfiguration<McqBookmark>
{
    public void Configure(EntityTypeBuilder<McqBookmark> builder)
    {
        builder.ToTable("McqBookmarks");
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

internal sealed class McqMistakeConfiguration : IEntityTypeConfiguration<McqMistake>
{
    public void Configure(EntityTypeBuilder<McqMistake> builder)
    {
        builder.ToTable("McqMistakes");
        builder.HasKey(mistake => new { mistake.UserId, mistake.QuestionId });
        builder.Property(mistake => mistake.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(mistake => mistake.Notes).HasMaxLength(2000);
        builder.Property(mistake => mistake.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(mistake => mistake.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(mistake => new { mistake.UserId, mistake.Status, mistake.UpdatedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(mistake => mistake.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mistake => mistake.Question)
            .WithMany()
            .HasForeignKey(mistake => mistake.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(mistake => mistake.LastAttempt)
            .WithMany()
            .HasForeignKey(mistake => mistake.LastAttemptId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

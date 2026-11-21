using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class VivaCategoryConfiguration : IEntityTypeConfiguration<VivaCategory>
{
    public void Configure(EntityTypeBuilder<VivaCategory> builder)
    {
        builder.ToTable("VivaCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Slug).HasMaxLength(80).IsRequired();
        builder.Property(category => category.Name).HasMaxLength(80).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(category => category.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => new { category.IsActive, category.DisplayOrder });
    }
}

internal sealed class VivaQuestionConfiguration : IEntityTypeConfiguration<VivaQuestion>
{
    public void Configure(EntityTypeBuilder<VivaQuestion> builder)
    {
        builder.ToTable("VivaQuestions");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.Topic).HasMaxLength(160);
        builder.Property(question => question.Prompt).HasMaxLength(10000).IsRequired();
        builder.Property(question => question.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(question => question.ExamReference).HasMaxLength(160);
        builder.Property(question => question.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(question => question.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(question => new { question.Status, question.CategoryId });
        builder.HasIndex(question => new { question.Topic, question.Status });
        builder.HasIndex(question => question.BcsExamYear);
        builder.HasOne(question => question.Category)
            .WithMany(category => category.Questions)
            .HasForeignKey(question => question.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(question => question.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VivaQuestionReferenceConfiguration : IEntityTypeConfiguration<VivaQuestionReference>
{
    public void Configure(EntityTypeBuilder<VivaQuestionReference> builder)
    {
        builder.ToTable("VivaQuestionReferences");
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(reference => reference.Title).HasMaxLength(300).IsRequired();
        builder.Property(reference => reference.Publisher).HasMaxLength(180).IsRequired();
        builder.Property(reference => reference.Url).HasMaxLength(512).IsRequired();
        builder.Property(reference => reference.Citation).HasMaxLength(500);
        builder.Property(reference => reference.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(reference => new { reference.QuestionId, reference.Type });
        builder.HasOne(reference => reference.Question)
            .WithMany(question => question.References)
            .HasForeignKey(reference => reference.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class VivaBookmarkConfiguration : IEntityTypeConfiguration<VivaBookmark>
{
    public void Configure(EntityTypeBuilder<VivaBookmark> builder)
    {
        builder.ToTable("VivaBookmarks");
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

internal sealed class VivaQuestionHistoryConfiguration : IEntityTypeConfiguration<VivaQuestionHistory>
{
    public void Configure(EntityTypeBuilder<VivaQuestionHistory> builder)
    {
        builder.ToTable("VivaQuestionHistory");
        builder.HasKey(history => new { history.UserId, history.QuestionId });
        builder.Property(history => history.LastViewedAt).HasColumnType("datetime(6)");
        builder.HasIndex(history => new { history.UserId, history.LastViewedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(history => history.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(history => history.Question)
            .WithMany()
            .HasForeignKey(history => history.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VivaAnswerNoteConfiguration : IEntityTypeConfiguration<VivaAnswerNote>
{
    public void Configure(EntityTypeBuilder<VivaAnswerNote> builder)
    {
        builder.ToTable("VivaAnswerNotes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Content).HasColumnType("longtext").IsRequired();
        builder.Property(note => note.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(note => note.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(note => new { note.UserId, note.QuestionId }).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(note => note.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(note => note.Question)
            .WithMany()
            .HasForeignKey(note => note.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VivaAnswerNoteRevisionConfiguration : IEntityTypeConfiguration<VivaAnswerNoteRevision>
{
    public void Configure(EntityTypeBuilder<VivaAnswerNoteRevision> builder)
    {
        builder.ToTable("VivaAnswerNoteRevisions");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Content).HasColumnType("longtext").IsRequired();
        builder.Property(revision => revision.CreatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(revision => new { revision.AnswerNoteId, revision.RevisionNumber }).IsUnique();
        builder.HasOne(revision => revision.AnswerNote)
            .WithMany(note => note.Revisions)
            .HasForeignKey(revision => revision.AnswerNoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class VivaMockSessionConfiguration : IEntityTypeConfiguration<VivaMockSession>
{
    public void Configure(EntityTypeBuilder<VivaMockSession> builder)
    {
        builder.ToTable("VivaMockSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Title).HasMaxLength(160).IsRequired();
        builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(session => session.FeedbackNotes).HasMaxLength(4000);
        builder.Property(session => session.ImprovementAreas).HasMaxLength(4000);
        builder.Property(session => session.StartedAt).HasColumnType("datetime(6)");
        builder.Property(session => session.CompletedAt).HasColumnType("datetime(6)");
        builder.HasIndex(session => new { session.UserId, session.StartedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class VivaMockSessionQuestionConfiguration : IEntityTypeConfiguration<VivaMockSessionQuestion>
{
    public void Configure(EntityTypeBuilder<VivaMockSessionQuestion> builder)
    {
        builder.ToTable("VivaMockSessionQuestions");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.PromptSnapshot).HasMaxLength(10000).IsRequired();
        builder.Property(question => question.CategorySnapshot).HasMaxLength(80).IsRequired();
        builder.Property(question => question.TopicSnapshot).HasMaxLength(160);
        builder.Property(question => question.AnswerNotes).HasColumnType("longtext").IsRequired();
        builder.HasIndex(question => new { question.SessionId, question.Sequence }).IsUnique();
        builder.HasIndex(question => new { question.SessionId, question.QuestionId }).IsUnique();
        builder.HasIndex(question => new { question.NeedsRevision, question.TopicSnapshot });
        builder.HasOne(question => question.Session)
            .WithMany(session => session.Questions)
            .HasForeignKey(question => question.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(question => question.Question)
            .WithMany()
            .HasForeignKey(question => question.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VivaProfilePreparationConfiguration : IEntityTypeConfiguration<VivaProfilePreparation>
{
    public void Configure(EntityTypeBuilder<VivaProfilePreparation> builder)
    {
        builder.ToTable("VivaProfilePreparations");
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.SelfIntroduction).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.AcademicBackground).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.UniversityAndSubject).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.HomeDistrict).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.ProfessionalExperience).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.CadrePreferences).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.InterestsAndHobbies).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.StrengthsAndDevelopmentAreas).HasColumnType("longtext").IsRequired();
        builder.Property(profile => profile.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

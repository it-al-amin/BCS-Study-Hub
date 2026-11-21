using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class CurrentAffairsEntryConfiguration : IEntityTypeConfiguration<CurrentAffairsEntry>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntry> builder)
    {
        builder.ToTable("CurrentAffairsEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Title).HasMaxLength(300).IsRequired();
        builder.Property(entry => entry.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(entry => entry.Content).HasColumnType("longtext");
        builder.Property(entry => entry.EventDate).HasColumnType("date");
        builder.Property(entry => entry.SourcePublishedOn).HasColumnType("date");
        builder.Property(entry => entry.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(entry => entry.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(entry => entry.SourceName).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.SourceUrl).HasMaxLength(1024).IsRequired();
        builder.Property(entry => entry.SourceStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(entry => entry.Notes).HasMaxLength(4000);
        builder.Property(entry => entry.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(entry => entry.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(entry => entry.PublishedAt).HasColumnType("datetime(6)");
        builder.HasIndex(entry => new { entry.Status, entry.EventDate });
        builder.HasIndex(entry => new { entry.Status, entry.Category, entry.EventDate });
        builder.HasIndex(entry => entry.Title);
        builder.HasOne(entry => entry.OfficialSource)
            .WithMany()
            .HasForeignKey(entry => entry.OfficialSourceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(entry => entry.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CurrentAffairsEntryTagConfiguration : IEntityTypeConfiguration<CurrentAffairsEntryTag>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntryTag> builder)
    {
        builder.ToTable("CurrentAffairsEntryTags");
        builder.HasKey(tag => new { tag.EntryId, tag.Tag });
        builder.Property(tag => tag.Tag).HasMaxLength(60);
        builder.HasIndex(tag => tag.Tag);
        builder.HasOne(tag => tag.Entry).WithMany(entry => entry.Tags).HasForeignKey(tag => tag.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CurrentAffairsEntrySubjectConfiguration : IEntityTypeConfiguration<CurrentAffairsEntrySubject>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntrySubject> builder)
    {
        builder.ToTable("CurrentAffairsEntrySubjects");
        builder.HasKey(link => new { link.EntryId, link.SubjectId });
        builder.HasIndex(link => link.SubjectId);
        builder.HasOne(link => link.Entry).WithMany(entry => entry.Subjects).HasForeignKey(link => link.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.Subject).WithMany().HasForeignKey(link => link.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CurrentAffairsEntryTopicConfiguration : IEntityTypeConfiguration<CurrentAffairsEntryTopic>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntryTopic> builder)
    {
        builder.ToTable("CurrentAffairsEntryTopics");
        builder.HasKey(link => new { link.EntryId, link.SyllabusItemId });
        builder.HasIndex(link => link.SyllabusItemId);
        builder.HasOne(link => link.Entry).WithMany(entry => entry.Topics).HasForeignKey(link => link.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.SyllabusItem).WithMany().HasForeignKey(link => link.SyllabusItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CurrentAffairsEntryQuestionConfiguration : IEntityTypeConfiguration<CurrentAffairsEntryQuestion>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntryQuestion> builder)
    {
        builder.ToTable("CurrentAffairsEntryQuestions");
        builder.HasKey(link => new { link.EntryId, link.QuestionType, link.QuestionId });
        builder.Property(link => link.QuestionType).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(link => new { link.QuestionType, link.QuestionId });
        builder.HasOne(link => link.Entry).WithMany(entry => entry.Questions).HasForeignKey(link => link.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CurrentAffairsEntryReferenceConfiguration : IEntityTypeConfiguration<CurrentAffairsEntryReference>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntryReference> builder)
    {
        builder.ToTable("CurrentAffairsEntryReferences");
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(reference => reference.Title).HasMaxLength(300).IsRequired();
        builder.Property(reference => reference.Url).HasMaxLength(1024).IsRequired();
        builder.HasIndex(reference => reference.EntryId);
        builder.HasOne(reference => reference.Entry).WithMany(entry => entry.References).HasForeignKey(reference => reference.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CurrentAffairsEntryResourceConfiguration : IEntityTypeConfiguration<CurrentAffairsEntryResource>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsEntryResource> builder)
    {
        builder.ToTable("CurrentAffairsEntryResources");
        builder.HasKey(link => new { link.EntryId, link.ResourceId });
        builder.HasIndex(link => link.ResourceId);
        builder.HasOne(link => link.Entry).WithMany(entry => entry.LibraryResources).HasForeignKey(link => link.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.Resource).WithMany().HasForeignKey(link => link.ResourceId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CurrentAffairsBookmarkConfiguration : IEntityTypeConfiguration<CurrentAffairsBookmark>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsBookmark> builder)
    {
        builder.ToTable("CurrentAffairsBookmarks");
        builder.HasKey(bookmark => new { bookmark.UserId, bookmark.EntryId });
        builder.Property(bookmark => bookmark.CreatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(bookmark => new { bookmark.UserId, bookmark.CreatedAt });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(bookmark => bookmark.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bookmark => bookmark.Entry).WithMany().HasForeignKey(bookmark => bookmark.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CurrentAffairsStudyNoteConfiguration : IEntityTypeConfiguration<CurrentAffairsStudyNote>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsStudyNote> builder)
    {
        builder.ToTable("CurrentAffairsStudyNotes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Content).HasColumnType("longtext").IsRequired();
        builder.Property(note => note.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(note => note.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(note => new { note.UserId, note.EntryId }).IsUnique();
        builder.HasIndex(note => new { note.UserId, note.UpdatedAt });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(note => note.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(note => note.Entry).WithMany().HasForeignKey(note => note.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CurrentAffairsRevisionConfiguration : IEntityTypeConfiguration<CurrentAffairsRevision>
{
    public void Configure(EntityTypeBuilder<CurrentAffairsRevision> builder)
    {
        builder.ToTable("CurrentAffairsRevisions");
        builder.HasKey(revision => new { revision.UserId, revision.EntryId });
        builder.Property(revision => revision.NextReviewOn).HasColumnType("date");
        builder.Property(revision => revision.LastReviewedAt).HasColumnType("datetime(6)");
        builder.Property(revision => revision.AddedAt).HasColumnType("datetime(6)");
        builder.Property(revision => revision.LastOutcome).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(revision => new { revision.UserId, revision.NextReviewOn });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(revision => revision.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(revision => revision.Entry).WithMany().HasForeignKey(revision => revision.EntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OfficialSourceConfiguration : IEntityTypeConfiguration<OfficialSource>
{
    public void Configure(EntityTypeBuilder<OfficialSource> builder)
    {
        builder.ToTable("OfficialSources");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.Name).HasMaxLength(200).IsRequired();
        builder.Property(source => source.LocalizedName).HasMaxLength(200);
        builder.Property(source => source.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(source => source.Url).HasMaxLength(512).IsRequired();
        builder.Property(source => source.Description).HasMaxLength(2000);
        builder.Property(source => source.ParentOrganization).HasMaxLength(200);
        builder.Property(source => source.UsageNotes).HasMaxLength(2000);
        builder.Property(source => source.VerificationStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(source => source.LastVerifiedOn).HasColumnType("date");
        builder.Property(source => source.VerificationNote).HasMaxLength(1000);
        builder.Property(source => source.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(source => source.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(source => source.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(source => source.Url).IsUnique();
        builder.HasIndex(source => new { source.Status, source.Category, source.SortOrder });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(source => source.VerifiedById).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class OfficialSourceTagConfiguration : IEntityTypeConfiguration<OfficialSourceTag>
{
    public void Configure(EntityTypeBuilder<OfficialSourceTag> builder)
    {
        builder.ToTable("OfficialSourceTags");
        builder.HasKey(tag => new { tag.SourceId, tag.Tag });
        builder.Property(tag => tag.Tag).HasMaxLength(60);
        builder.HasOne(tag => tag.Source).WithMany(source => source.Tags).HasForeignKey(tag => tag.SourceId).OnDelete(DeleteBehavior.Cascade);
    }
}

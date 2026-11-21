using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class LibraryResourceConfiguration : IEntityTypeConfiguration<LibraryResource>
{
    public void Configure(EntityTypeBuilder<LibraryResource> builder)
    {
        builder.ToTable("LibraryResources");
        builder.HasKey(resource => resource.Id);
        builder.Property(resource => resource.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(resource => resource.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(resource => resource.Visibility).HasConversion<string>().HasMaxLength(16);
        builder.Property(resource => resource.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(resource => resource.SourceType).HasConversion<string>().HasMaxLength(32);
        builder.Property(resource => resource.RightsBasis).HasConversion<string>().HasMaxLength(32);
        builder.Property(resource => resource.Title).HasMaxLength(300).IsRequired();
        builder.Property(resource => resource.Description).HasMaxLength(4000);
        builder.Property(resource => resource.Authors).HasMaxLength(500);
        builder.Property(resource => resource.SourceName).HasMaxLength(200);
        builder.Property(resource => resource.Website).HasMaxLength(200);
        builder.Property(resource => resource.Url).HasMaxLength(1024);
        builder.Property(resource => resource.PublicationDate).HasColumnType("date");
        builder.Property(resource => resource.RightsNote).HasMaxLength(1000);
        builder.Property(resource => resource.NoteContent).HasColumnType("longtext");
        builder.Property(resource => resource.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(resource => resource.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(resource => resource.PublishedAt).HasColumnType("datetime(6)");
        builder.HasIndex(resource => new { resource.Visibility, resource.Status, resource.Kind });
        builder.HasIndex(resource => new { resource.OwnerId, resource.Visibility });
        builder.HasIndex(resource => resource.Title);
        builder.HasIndex(resource => resource.UpdatedAt);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(resource => resource.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LibraryBookDetailsConfiguration : IEntityTypeConfiguration<LibraryBookDetails>
{
    public void Configure(EntityTypeBuilder<LibraryBookDetails> builder)
    {
        builder.ToTable("LibraryBookDetails");
        builder.HasKey(book => book.ResourceId);
        builder.Property(book => book.Publisher).HasMaxLength(200);
        builder.Property(book => book.Edition).HasMaxLength(80);
        builder.Property(book => book.Isbn).HasMaxLength(13);
        builder.Property(book => book.Language).HasMaxLength(60);
        builder.HasIndex(book => book.Isbn);
        builder.HasOne(book => book.Resource)
            .WithOne(resource => resource.Book)
            .HasForeignKey<LibraryBookDetails>(book => book.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LibraryFileConfiguration : IEntityTypeConfiguration<LibraryFile>
{
    public void Configure(EntityTypeBuilder<LibraryFile> builder)
    {
        builder.ToTable("LibraryFiles");
        builder.HasKey(file => file.Id);
        builder.Property(file => file.Role).HasConversion<string>().HasMaxLength(16);
        builder.Property(file => file.Format).HasConversion<string>().HasMaxLength(16);
        builder.Property(file => file.OriginalFileName).HasMaxLength(200).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(file => file.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(file => file.StorageProvider).HasMaxLength(40).IsRequired();
        builder.Property(file => file.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(file => file.UploadedAt).HasColumnType("datetime(6)");
        builder.HasIndex(file => new { file.ResourceId, file.Role }).IsUnique();
        builder.HasIndex(file => file.StorageKey).IsUnique();
        builder.HasIndex(file => file.UploadedById);
        builder.HasOne(file => file.Resource)
            .WithMany(resource => resource.Files)
            .HasForeignKey(file => file.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(file => file.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LibraryBookChapterConfiguration : IEntityTypeConfiguration<LibraryBookChapter>
{
    public void Configure(EntityTypeBuilder<LibraryBookChapter> builder)
    {
        builder.ToTable("LibraryBookChapters");
        builder.HasKey(chapter => chapter.Id);
        builder.Property(chapter => chapter.Title).HasMaxLength(300).IsRequired();
        builder.Property(chapter => chapter.Summary).HasMaxLength(4000);
        builder.HasIndex(chapter => new { chapter.ResourceId, chapter.ChapterNumber }).IsUnique();
        builder.HasOne(chapter => chapter.Resource)
            .WithMany(resource => resource.Chapters)
            .HasForeignKey(chapter => chapter.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LibraryChapterTopicConfiguration : IEntityTypeConfiguration<LibraryChapterTopic>
{
    public void Configure(EntityTypeBuilder<LibraryChapterTopic> builder)
    {
        builder.ToTable("LibraryChapterTopics");
        builder.HasKey(link => new { link.ChapterId, link.SyllabusItemId });
        builder.HasIndex(link => link.SyllabusItemId);
        builder.HasOne(link => link.Chapter)
            .WithMany(chapter => chapter.Topics)
            .HasForeignKey(link => link.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.SyllabusItem)
            .WithMany()
            .HasForeignKey(link => link.SyllabusItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LibraryChapterResourceConfiguration : IEntityTypeConfiguration<LibraryChapterResource>
{
    public void Configure(EntityTypeBuilder<LibraryChapterResource> builder)
    {
        builder.ToTable("LibraryChapterResources");
        builder.HasKey(link => new { link.ChapterId, link.ResourceId });
        builder.HasIndex(link => link.ResourceId);
        builder.HasOne(link => link.Chapter)
            .WithMany(chapter => chapter.LinkedResources)
            .HasForeignKey(link => link.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.Resource)
            .WithMany()
            .HasForeignKey(link => link.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LibraryResourceSubjectConfiguration : IEntityTypeConfiguration<LibraryResourceSubject>
{
    public void Configure(EntityTypeBuilder<LibraryResourceSubject> builder)
    {
        builder.ToTable("LibraryResourceSubjects");
        builder.HasKey(link => new { link.ResourceId, link.SubjectId });
        builder.HasIndex(link => link.SubjectId);
        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.Subjects)
            .HasForeignKey(link => link.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.Subject)
            .WithMany()
            .HasForeignKey(link => link.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LibraryResourceTopicConfiguration : IEntityTypeConfiguration<LibraryResourceTopic>
{
    public void Configure(EntityTypeBuilder<LibraryResourceTopic> builder)
    {
        builder.ToTable("LibraryResourceTopics");
        builder.HasKey(link => new { link.ResourceId, link.SyllabusItemId });
        builder.HasIndex(link => link.SyllabusItemId);
        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.Topics)
            .HasForeignKey(link => link.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.SyllabusItem)
            .WithMany()
            .HasForeignKey(link => link.SyllabusItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LibraryResourceTagConfiguration : IEntityTypeConfiguration<LibraryResourceTag>
{
    public void Configure(EntityTypeBuilder<LibraryResourceTag> builder)
    {
        builder.ToTable("LibraryResourceTags");
        builder.HasKey(tag => new { tag.ResourceId, tag.Tag });
        builder.Property(tag => tag.Tag).HasMaxLength(60);
        builder.HasIndex(tag => tag.Tag);
        builder.HasOne(tag => tag.Resource)
            .WithMany(resource => resource.Tags)
            .HasForeignKey(tag => tag.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LibraryCollectionConfiguration : IEntityTypeConfiguration<LibraryCollection>
{
    public void Configure(EntityTypeBuilder<LibraryCollection> builder)
    {
        builder.ToTable("LibraryCollections");
        builder.HasKey(collection => collection.Id);
        builder.Property(collection => collection.Name).HasMaxLength(150).IsRequired();
        builder.Property(collection => collection.Description).HasMaxLength(1000);
        builder.Property(collection => collection.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(collection => collection.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(collection => new { collection.OwnerId, collection.Name }).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(collection => collection.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LibraryCollectionItemConfiguration : IEntityTypeConfiguration<LibraryCollectionItem>
{
    public void Configure(EntityTypeBuilder<LibraryCollectionItem> builder)
    {
        builder.ToTable("LibraryCollectionItems");
        builder.HasKey(item => new { item.CollectionId, item.ResourceId });
        builder.Property(item => item.AddedAt).HasColumnType("datetime(6)");
        builder.HasIndex(item => item.ResourceId);
        builder.HasOne(item => item.Collection)
            .WithMany(collection => collection.Items)
            .HasForeignKey(item => item.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Resource)
            .WithMany()
            .HasForeignKey(item => item.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LibraryBookmarkConfiguration : IEntityTypeConfiguration<LibraryBookmark>
{
    public void Configure(EntityTypeBuilder<LibraryBookmark> builder)
    {
        builder.ToTable("LibraryBookmarks");
        builder.HasKey(bookmark => bookmark.Id);
        builder.Property(bookmark => bookmark.Label).HasMaxLength(200);
        builder.Property(bookmark => bookmark.CreatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(bookmark => new { bookmark.UserId, bookmark.CreatedAt });
        builder.HasIndex(bookmark => new { bookmark.UserId, bookmark.ResourceId });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(bookmark => bookmark.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bookmark => bookmark.Resource)
            .WithMany()
            .HasForeignKey(bookmark => bookmark.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bookmark => bookmark.Chapter)
            .WithMany()
            .HasForeignKey(bookmark => bookmark.ChapterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class LibraryReadingProgressConfiguration : IEntityTypeConfiguration<LibraryReadingProgress>
{
    public void Configure(EntityTypeBuilder<LibraryReadingProgress> builder)
    {
        builder.ToTable("LibraryReadingProgress");
        builder.HasKey(progress => new { progress.UserId, progress.ResourceId });
        builder.Property(progress => progress.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(progress => progress.StartedAt).HasColumnType("datetime(6)");
        builder.Property(progress => progress.CompletedAt).HasColumnType("datetime(6)");
        builder.Property(progress => progress.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(progress => new { progress.UserId, progress.Status, progress.UpdatedAt });
        builder.HasIndex(progress => progress.ResourceId);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(progress => progress.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(progress => progress.Resource)
            .WithMany()
            .HasForeignKey(progress => progress.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(progress => progress.CurrentChapter)
            .WithMany()
            .HasForeignKey(progress => progress.CurrentChapterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class LibraryPersonalNoteConfiguration : IEntityTypeConfiguration<LibraryPersonalNote>
{
    public void Configure(EntityTypeBuilder<LibraryPersonalNote> builder)
    {
        builder.ToTable("LibraryPersonalNotes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Content).HasColumnType("longtext").IsRequired();
        builder.Property(note => note.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(note => note.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(note => new { note.UserId, note.ResourceId, note.UpdatedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(note => note.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(note => note.Resource)
            .WithMany()
            .HasForeignKey(note => note.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(note => note.Chapter)
            .WithMany()
            .HasForeignKey(note => note.ChapterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

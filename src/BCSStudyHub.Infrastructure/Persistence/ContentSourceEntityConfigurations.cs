using BCSStudyHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BCSStudyHub.Infrastructure.Persistence;

internal sealed class ContentSourceConfiguration : IEntityTypeConfiguration<ContentSource>
{
    public void Configure(EntityTypeBuilder<ContentSource> builder)
    {
        builder.ToTable("ContentSources");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.Name).HasMaxLength(200).IsRequired();
        builder.Property(source => source.LocalizedName).HasMaxLength(200);
        builder.Property(source => source.SourceType).HasConversion<string>().HasMaxLength(24);
        builder.Property(source => source.ExtractionMethod).HasConversion<string>().HasMaxLength(24);
        builder.Property(source => source.BaseUrl).HasMaxLength(512).IsRequired();
        builder.Property(source => source.FeedUrl).HasMaxLength(512);
        builder.Property(source => source.SitemapUrl).HasMaxLength(512);
        builder.Property(source => source.Category).HasMaxLength(60).IsRequired();
        builder.Property(source => source.Language).HasMaxLength(8).IsRequired();
        builder.Property(source => source.RetentionPolicy).HasConversion<string>().HasMaxLength(24);
        builder.Property(source => source.IncludePatterns).HasMaxLength(2000);
        builder.Property(source => source.ExcludePatterns).HasMaxLength(2000);
        builder.Property(source => source.RightsNote).HasMaxLength(1000);
        builder.Property(source => source.Notes).HasMaxLength(2000);
        builder.Property(source => source.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(source => source.Health).HasConversion<string>().HasMaxLength(16);
        builder.Property(source => source.LastError).HasMaxLength(1000);
        builder.Property(source => source.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(source => source.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(source => source.LastScanAt).HasColumnType("datetime(6)");
        builder.Property(source => source.LastSuccessAt).HasColumnType("datetime(6)");
        builder.HasIndex(source => source.BaseUrl).IsUnique();
        builder.HasIndex(source => new { source.Status, source.IsEnabled, source.Priority });
        builder.HasIndex(source => source.Category);
    }
}

internal sealed class ContentSourceScanConfiguration : IEntityTypeConfiguration<ContentSourceScan>
{
    public void Configure(EntityTypeBuilder<ContentSourceScan> builder)
    {
        builder.ToTable("ContentSourceScans");
        builder.HasKey(scan => scan.Id);
        builder.Property(scan => scan.Trigger).HasConversion<string>().HasMaxLength(16);
        builder.Property(scan => scan.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(scan => scan.ErrorMessage).HasMaxLength(1000);
        builder.Property(scan => scan.RequestedAt).HasColumnType("datetime(6)");
        builder.Property(scan => scan.StartedAt).HasColumnType("datetime(6)");
        builder.Property(scan => scan.CompletedAt).HasColumnType("datetime(6)");
        builder.HasOne(scan => scan.Source).WithMany(source => source.Scans).HasForeignKey(scan => scan.SourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(scan => new { scan.SourceId, scan.RequestedAt });
        builder.HasIndex(scan => new { scan.Status, scan.RequestedAt });
    }
}

internal sealed class DiscoveredContentConfiguration : IEntityTypeConfiguration<DiscoveredContent>
{
    public void Configure(EntityTypeBuilder<DiscoveredContent> builder)
    {
        builder.ToTable("DiscoveredContents");
        builder.HasKey(content => content.Id);
        builder.Property(content => content.CanonicalUrl).HasMaxLength(1000).IsRequired();
        builder.Property(content => content.UrlHash).HasMaxLength(64).IsRequired();
        builder.Property(content => content.Title).HasMaxLength(500).IsRequired();
        builder.Property(content => content.Language).HasMaxLength(16);
        builder.Property(content => content.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(content => content.Excerpt).HasMaxLength(600);
        builder.Property(content => content.TextContent).HasColumnType("longtext");
        builder.Property(content => content.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(content => content.ETag).HasMaxLength(200);
        builder.Property(content => content.LastModifiedHeader).HasMaxLength(100);
        builder.Property(content => content.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(content => content.Visibility).HasConversion<string>().HasMaxLength(16);
        builder.Property(content => content.PublishedAt).HasColumnType("datetime(6)");
        builder.Property(content => content.DiscoveredAt).HasColumnType("datetime(6)");
        builder.Property(content => content.LastSeenAt).HasColumnType("datetime(6)");
        builder.Property(content => content.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasOne(content => content.Source).WithMany().HasForeignKey(content => content.SourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(content => new { content.SourceId, content.UrlHash }).IsUnique();
        builder.HasIndex(content => content.ContentHash);
        builder.HasIndex(content => new { content.Status, content.DiscoveredAt });
    }
}

internal sealed class DiscoveredContentVersionConfiguration : IEntityTypeConfiguration<DiscoveredContentVersion>
{
    public void Configure(EntityTypeBuilder<DiscoveredContentVersion> builder)
    {
        builder.ToTable("DiscoveredContentVersions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Title).HasMaxLength(500).IsRequired();
        builder.Property(version => version.Excerpt).HasMaxLength(600);
        builder.Property(version => version.TextContent).HasColumnType("longtext");
        builder.Property(version => version.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(version => version.CapturedAt).HasColumnType("datetime(6)");
        builder.HasOne(version => version.Content).WithMany(content => content.Versions).HasForeignKey(version => version.ContentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(version => new { version.ContentId, version.Version }).IsUnique();
    }
}

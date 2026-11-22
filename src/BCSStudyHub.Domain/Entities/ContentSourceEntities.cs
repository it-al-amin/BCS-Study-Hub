namespace BCSStudyHub.Domain.Entities;

public enum ContentSourceType
{
    Website,
    RssFeed,
    AtomFeed,
    Sitemap,
    OfficialApi,
    DocumentRepository,
    UserProvided,
}

/// <summary>How content is acquired from the source. Only methods the source's terms and robots.txt permit may be used.</summary>
public enum ContentExtractionMethod
{
    ManualOnly,
    Feed,
    Sitemap,
    HtmlExtraction,
    DocumentDiscovery,
    Api,
}

/// <summary>What may be kept from a source: this limits extraction, not just display.</summary>
public enum ContentRetentionPolicy
{
    LinkAndMetadataOnly,
    ExcerptAndSummary,
    FullTextPermitted,
}

public enum ContentSourceStatus
{
    Active,
    Archived,
}

public enum ContentSourceHealth
{
    NeverScanned,
    Healthy,
    NeedsAttention,
    Failing,
}

public enum ContentScanStatus
{
    Queued,
    Running,
    Succeeded,
    PartiallySucceeded,
    Failed,
    Cancelled,
}

public enum ContentScanTrigger
{
    Scheduled,
    Manual,
    Retry,
}

public sealed class ContentSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LocalizedName { get; set; }
    public ContentSourceType SourceType { get; set; }
    public ContentExtractionMethod ExtractionMethod { get; set; } = ContentExtractionMethod.ManualOnly;
    public string BaseUrl { get; set; } = string.Empty;
    public string? FeedUrl { get; set; }
    public string? SitemapUrl { get; set; }

    /// <summary>Configurable grouping such as "Official", "News", "Education"; a string so admins can edit it.</summary>
    public string Category { get; set; } = "General";

    /// <summary>Content language: "bn", "en" or "mixed".</summary>
    public string Language { get; set; } = "bn";

    /// <summary>Lower numbers are scanned first; official sources use low numbers.</summary>
    public int Priority { get; set; } = 100;

    /// <summary>1 (unreliable) to 5 (authoritative).</summary>
    public int Reliability { get; set; } = 3;

    public int ScanIntervalMinutes { get; set; } = 1440;
    public ContentRetentionPolicy RetentionPolicy { get; set; } = ContentRetentionPolicy.LinkAndMetadataOnly;

    /// <summary>One URL pattern per line. An empty include list means "everything on the host".</summary>
    public string? IncludePatterns { get; set; }
    public string? ExcludePatterns { get; set; }

    public bool RequiresVerification { get; set; } = true;
    public string? RightsNote { get; set; }
    public string? Notes { get; set; }
    public bool IsEnabled { get; set; }
    public ContentSourceStatus Status { get; set; } = ContentSourceStatus.Active;
    public ContentSourceHealth Health { get; set; } = ContentSourceHealth.NeverScanned;
    public DateTimeOffset? LastScanAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
    public int ConsecutiveFailures { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ContentSourceScan> Scans { get; set; } = [];
}

public sealed class ContentSourceScan
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public ContentSource Source { get; set; } = null!;
    public ContentScanTrigger Trigger { get; set; }
    public ContentScanStatus Status { get; set; } = ContentScanStatus.Queued;
    public DateTimeOffset RequestedAt { get; set; }
    public Guid? RequestedById { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int DiscoveredCount { get; set; }
    public int UpdatedCount { get; set; }
    public int UnchangedCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorMessage { get; set; }
}

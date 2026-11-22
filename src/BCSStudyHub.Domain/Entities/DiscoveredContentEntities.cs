namespace BCSStudyHub.Domain.Entities;

/// <summary>Review workflow: Pending Review -> Verified -> Published, plus the terminal and exceptional states.</summary>
public enum DiscoveredContentStatus
{
    PendingReview,
    Discovered,
    Updated,
    Verified,
    Published,
    Rejected,
    Archived,
    Duplicate,
    Unavailable,
    FailedProcessing,
}

public enum ContentVisibility
{
    Private,
    Shared,
    Published,
}

public enum DiscoveredContentKind
{
    Article,
    Document,
    Notice,
    Other,
}

/// <summary>An item a scan found at a configured source. Private and unreviewed until someone verifies it.</summary>
public sealed class DiscoveredContent
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public ContentSource Source { get; set; } = null!;

    /// <summary>Normalised URL (lower-case host, no fragment or tracking parameters).</summary>
    public string CanonicalUrl { get; set; } = string.Empty;

    /// <summary>SHA-256 (hex) of <see cref="CanonicalUrl"/>; with <see cref="SourceId"/> it identifies the item.</summary>
    public string UrlHash { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Language { get; set; }
    public DiscoveredContentKind Kind { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Short excerpt kept when the source's retention policy allows it.</summary>
    public string? Excerpt { get; set; }

    /// <summary>Full extracted text, only when the source's retention policy is FullTextPermitted.</summary>
    public string? TextContent { get; set; }

    /// <summary>SHA-256 (hex) of the normalised title and text, used to detect real content changes.</summary>
    public string ContentHash { get; set; } = string.Empty;

    public string? ETag { get; set; }
    public string? LastModifiedHeader { get; set; }
    public int Version { get; set; } = 1;
    public DiscoveredContentStatus Status { get; set; } = DiscoveredContentStatus.PendingReview;
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Private;
    public Guid? DuplicateOfId { get; set; }
    public DateTimeOffset DiscoveredAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<DiscoveredContentVersion> Versions { get; set; } = [];
}

/// <summary>A captured state of an item. Earlier versions are kept so changes are never overwritten silently.</summary>
public sealed class DiscoveredContentVersion
{
    public Guid Id { get; set; }
    public Guid ContentId { get; set; }
    public DiscoveredContent Content { get; set; } = null!;
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? TextContent { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; set; }
    public Guid? ScanId { get; set; }
}

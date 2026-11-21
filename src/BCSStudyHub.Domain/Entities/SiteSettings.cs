namespace BCSStudyHub.Domain.Entities;

public enum AnnouncementTone
{
    Info,
    Warning,
    Critical,
}

/// <summary>Single-row, typed site configuration (Id is always 1).</summary>
public sealed class SiteSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string SiteName { get; set; } = "BCS Study Hub";
    public string? Tagline { get; set; }
    public bool AnnouncementEnabled { get; set; }
    public string? AnnouncementText { get; set; }
    public AnnouncementTone AnnouncementTone { get; set; } = AnnouncementTone.Info;
    public DateTimeOffset? AnnouncementStartsAt { get; set; }
    public DateTimeOffset? AnnouncementEndsAt { get; set; }
    public string? SupportEmail { get; set; }

    /// <summary>Enforced by the registration endpoint, not only hidden in the UI.</summary>
    public bool RegistrationOpen { get; set; } = true;

    public string? FooterNotice { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

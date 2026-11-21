namespace BCSStudyHub.Domain.Entities;

public enum SourceVerificationStatus
{
    Unverified,
    Verified,
    Superseded,
}

public sealed class SyllabusSourceReference
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateOnly? PublishedOn { get; set; }
    public DateOnly? LastVerifiedOn { get; set; }
    public SourceVerificationStatus VerificationStatus { get; set; } = SourceVerificationStatus.Unverified;
    public string? Notes { get; set; }
    public ICollection<SyllabusVersionSource> SyllabusVersions { get; set; } = [];
    public ICollection<SyllabusItemSource> SyllabusItems { get; set; } = [];
}

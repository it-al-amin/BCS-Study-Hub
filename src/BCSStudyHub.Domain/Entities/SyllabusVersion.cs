namespace BCSStudyHub.Domain.Entities;

public enum SyllabusVersionStatus
{
    Draft,
    Published,
    Archived,
}

public enum SyllabusCoverage
{
    OutlineOnly,
    FullTopics,
}

public sealed class SyllabusVersion
{
    public Guid Id { get; set; }
    public Guid ExamStageId { get; set; }
    public ExamStage ExamStage { get; set; } = null!;
    public string VersionLabel { get; set; } = string.Empty;
    public string TrackCode { get; set; } = "Common";
    public string Summary { get; set; } = string.Empty;
    public SyllabusCoverage Coverage { get; set; } = SyllabusCoverage.OutlineOnly;
    public SyllabusVersionStatus Status { get; set; } = SyllabusVersionStatus.Draft;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<SyllabusSubject> Subjects { get; set; } = [];
    public ICollection<SyllabusVersionSource> SourceReferences { get; set; } = [];
}

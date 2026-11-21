namespace BCSStudyHub.Domain.Entities;

public sealed class ExamStage
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LocalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SyllabusVersion> SyllabusVersions { get; set; } = [];
}

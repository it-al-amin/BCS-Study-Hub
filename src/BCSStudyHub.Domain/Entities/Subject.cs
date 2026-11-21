namespace BCSStudyHub.Domain.Entities;

public sealed class Subject
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SyllabusSubject> SyllabusVersions { get; set; } = [];
}

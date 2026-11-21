namespace BCSStudyHub.Domain.Entities;

public sealed class SyllabusSubject
{
    public Guid Id { get; set; }
    public Guid SyllabusVersionId { get; set; }
    public SyllabusVersion SyllabusVersion { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int SortOrder { get; set; }
    public int? Marks { get; set; }
    public bool IsRequired { get; set; } = true;
    public ICollection<Paper> Papers { get; set; } = [];
    public ICollection<SyllabusItem> Items { get; set; } = [];
}

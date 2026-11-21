namespace BCSStudyHub.Domain.Entities;

public sealed class Paper
{
    public Guid Id { get; set; }
    public Guid SyllabusSubjectId { get; set; }
    public SyllabusSubject SyllabusSubject { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int? MaxMarks { get; set; }
    public ICollection<SyllabusItem> Items { get; set; } = [];
}

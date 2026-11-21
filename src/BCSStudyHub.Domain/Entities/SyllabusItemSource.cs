namespace BCSStudyHub.Domain.Entities;

public sealed class SyllabusItemSource
{
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
    public Guid SourceReferenceId { get; set; }
    public SyllabusSourceReference SourceReference { get; set; } = null!;
}

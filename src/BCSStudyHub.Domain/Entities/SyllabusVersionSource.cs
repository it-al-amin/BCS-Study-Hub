namespace BCSStudyHub.Domain.Entities;

public sealed class SyllabusVersionSource
{
    public Guid SyllabusVersionId { get; set; }
    public SyllabusVersion SyllabusVersion { get; set; } = null!;
    public Guid SourceReferenceId { get; set; }
    public SyllabusSourceReference SourceReference { get; set; } = null!;
    public string? Notes { get; set; }
}

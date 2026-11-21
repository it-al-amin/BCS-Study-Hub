namespace BCSStudyHub.Domain.Entities;

public sealed class TopicProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

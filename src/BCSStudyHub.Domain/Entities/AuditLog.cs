namespace BCSStudyHub.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? CorrelationId { get; set; }
}

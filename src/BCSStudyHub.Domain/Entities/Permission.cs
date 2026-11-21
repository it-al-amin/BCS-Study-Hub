namespace BCSStudyHub.Domain.Entities;

public sealed class Permission
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}

using Microsoft.AspNetCore.Identity;

namespace BCSStudyHub.Infrastructure.Identity;

public sealed class UserRoleMapping : IdentityUserRole<Guid>
{
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}

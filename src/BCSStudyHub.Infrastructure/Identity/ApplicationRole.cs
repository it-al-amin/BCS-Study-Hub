using Microsoft.AspNetCore.Identity;

namespace BCSStudyHub.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}

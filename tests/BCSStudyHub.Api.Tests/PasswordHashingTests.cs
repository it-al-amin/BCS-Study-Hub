using BCSStudyHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BCSStudyHub.Api.Tests;

public sealed class PasswordHashingTests
{
    [Fact]
    public void IdentityHasherStoresAndVerifiesOnlyAPasswordHash()
    {
        const string password = "Strong-Pass123!";
        var user = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Candidate" };
        var hasher = new PasswordHasher<ApplicationUser>();

        var hash = hasher.HashPassword(user, password);

        Assert.NotEqual(password, hash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, hash, password));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, hash, "Wrong-Pass123!"));
    }
}

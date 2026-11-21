using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Api.Controllers;

namespace BCSStudyHub.Api.Tests;

public sealed class AuthRequestValidationTests
{
    [Theory]
    [InlineData("candidate@example.org", "Strong-Pass123", "Candidate", true)]
    [InlineData("not-an-email", "Strong-Pass123", "Candidate", false)]
    [InlineData("candidate@example.org", "short", "Candidate", false)]
    [InlineData("candidate@example.org", "Strong-Pass123", "A", false)]
    public void RegistrationRequestValidatesEmailPasswordAndDisplayName(
        string email,
        string password,
        string displayName,
        bool expectedValid)
    {
        var request = new RegisterRequestBody
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void LoginRequestRequiresEmailAndPassword()
    {
        var request = new LoginRequestBody();
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(request.Email)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(request.Password)));
    }
}

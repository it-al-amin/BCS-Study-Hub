using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Syllabus;

namespace BCSStudyHub.Api.Tests;

public sealed class SyllabusRequestValidationTests
{
    [Theory]
    [InlineData("OutlineOnly", true)]
    [InlineData("FullTopics", true)]
    [InlineData("Complete", false)]
    public void VersionRequestAcceptsOnlySupportedCoverage(
        string coverage,
        bool expectedValid)
    {
        var request = new CreateSyllabusVersionRequest(
            "BPSC outline",
            "Common",
            "Source and coverage notes",
            coverage,
            null,
            null);

        Assert.Equal(expectedValid, IsValid(request));
    }

    [Theory]
    [InlineData("Unverified", true)]
    [InlineData("Verified", true)]
    [InlineData("NeedsReview", true)]
    [InlineData("Draft", false)]
    public void SyllabusItemRequestAcceptsOnlyKnownVerificationStatuses(
        string verificationStatus,
        bool expectedValid)
    {
        var request = new CreateSyllabusItemRequest(
            Guid.NewGuid(),
            null,
            null,
            "Subject heading",
            null,
            1,
            verificationStatus);

        Assert.Equal(expectedValid, IsValid(request));
    }

    private static bool IsValid(object instance)
    {
        var validationResults = new List<ValidationResult>();
        return Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            validationResults,
            validateAllProperties: true);
    }
}

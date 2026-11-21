using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Written;

namespace BCSStudyHub.Api.Tests;

public sealed class WrittenValidationTests
{
    [Fact]
    public void QuestionInputRejectsUnsupportedTypeAndMissingSourceAttribution()
    {
        var input = ValidQuestion() with
        {
            Type = "MultipleChoice",
            Sources = [],
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void QuestionInputRequiresReferencesForModelAnswers()
    {
        var input = ValidQuestion() with
        {
            ModelAnswerContent = "A model response.",
            ModelAnswerReferences = [],
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void SelfAssessmentRejectsOutOfRangeRatingsAndEmptyAssessments()
    {
        Assert.False(IsValid(new WrittenSelfAssessmentInput { Clarity = 6 }));
        Assert.False(IsValid(new WrittenSelfAssessmentInput()));
        Assert.True(IsValid(new WrittenSelfAssessmentInput { Reflection = "Review the structure." }));
    }

    [Fact]
    public void GoalRequiresValidOrderedDateRangeAndPositiveTargets()
    {
        Assert.False(IsValid(new WrittenGoalInput
        {
            StartDate = new DateOnly(2026, 3, 5),
            EndDate = new DateOnly(2026, 3, 4),
            TargetSubmissions = 2,
            TargetWords = 1000,
        }));
        Assert.False(IsValid(new WrittenGoalInput
        {
            StartDate = new DateOnly(2026, 3, 5),
            EndDate = new DateOnly(2026, 3, 10),
            TargetSubmissions = 0,
            TargetWords = 1000,
        }));
    }

    private static WrittenQuestionInput ValidQuestion() =>
        new()
        {
            SyllabusItemId = Guid.NewGuid(),
            Prompt = "Discuss a policy issue.",
            Type = "Essay",
            Sources =
            [
                new WrittenSourceInput
                {
                    Title = "Official examination reference",
                    Publisher = "BPSC",
                    Url = "https://bpsc.gov.bd/reference",
                    VerificationStatus = "Verified",
                },
            ],
        };

    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}

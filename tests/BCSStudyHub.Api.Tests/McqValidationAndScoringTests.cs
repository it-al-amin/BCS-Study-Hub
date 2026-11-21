using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Mcq;

namespace BCSStudyHub.Api.Tests;

public sealed class McqValidationAndScoringTests
{
    [Theory]
    [InlineData("SingleAnswer", 1, true)]
    [InlineData("SingleAnswer", 2, false)]
    [InlineData("MultipleAnswer", 1, false)]
    [InlineData("MultipleAnswer", 2, true)]
    public void QuestionInputValidatesCorrectOptionCardinality(
        string type,
        int correctCount,
        bool expectedValid)
    {
        var input = ValidQuestion() with
        {
            Type = type,
            Options =
            [
                new McqOptionInput { Text = "Option A", IsCorrect = correctCount > 0 },
                new McqOptionInput { Text = "Option B", IsCorrect = correctCount > 1 },
            ],
        };

        Assert.Equal(expectedValid, IsValid(input));
    }

    [Fact]
    public void QuestionInputRejectsDuplicateOptionsAndTags()
    {
        var input = ValidQuestion() with
        {
            Options =
            [
                new McqOptionInput { Text = "Same", IsCorrect = true },
                new McqOptionInput { Text = "Same", IsCorrect = false },
            ],
            Tags = ["core", "CORE"],
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void ExamSetInputRejectsUnspecifiedPracticeMode()
    {
        var input = new McqExamSetInput
        {
            SyllabusVersionId = Guid.NewGuid(),
            Name = "Practice",
            Mode = "Adaptive",
            QuestionCount = 10,
            MarksCorrect = 1,
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void ScoringUsesServerProvidedCorrectOptionsAndConfiguredMarks()
    {
        var correct = Guid.NewGuid();
        var wrong = Guid.NewGuid();

        Assert.Equal(
            new McqScoreResult(true, 1.25m),
            McqScoring.Evaluate([correct], [correct], 1.25m, -0.5m, 0m));
        Assert.Equal(
            new McqScoreResult(false, -0.5m),
            McqScoring.Evaluate([wrong], [correct], 1.25m, -0.5m, 0m));
        Assert.Equal(
            new McqScoreResult(null, 0m),
            McqScoring.Evaluate([], [correct], 1.25m, -0.5m, 0m));
    }

    [Fact]
    public void MultipleAnswerRequiresExactSetRatherThanPartialCredit()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var extra = Guid.NewGuid();

        Assert.Equal(
            new McqScoreResult(true, 2m),
            McqScoring.Evaluate([second, first], [first, second], 2m, -0.25m, 0m));
        Assert.Equal(
            new McqScoreResult(false, -0.25m),
            McqScoring.Evaluate([first, extra], [first, second], 2m, -0.25m, 0m));
    }

    private static McqQuestionInput ValidQuestion() =>
        new()
        {
            SyllabusItemId = Guid.NewGuid(),
            Prompt = "A sample question?",
            Type = "SingleAnswer",
            Difficulty = "Easy",
            Options =
            [
                new McqOptionInput { Text = "Option A", IsCorrect = true },
                new McqOptionInput { Text = "Option B", IsCorrect = false },
            ],
            Sources =
            [
                new McqQuestionSourceInput
                {
                    Title = "Reference",
                    Publisher = "Publisher",
                    Url = "https://example.org/reference",
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

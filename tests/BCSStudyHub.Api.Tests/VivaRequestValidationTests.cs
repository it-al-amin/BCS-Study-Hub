using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Viva;

namespace BCSStudyHub.Api.Tests;

public sealed class VivaRequestValidationTests
{
    [Fact]
    public void CategoryInputRequiresAnAsciiSlug()
    {
        Assert.False(IsValid(new VivaCategoryInput
        {
            Name = "Bangladesh affairs",
            Slug = "বাংলাদেশ-affairs",
            DisplayOrder = 1,
        }));
        Assert.True(IsValid(new VivaCategoryInput
        {
            Name = "Bangladesh affairs",
            Slug = "bangladesh-affairs",
            DisplayOrder = 1,
        }));
    }

    [Fact]
    public void QuestionInputRequiresCategoryAndPrompt()
    {
        Assert.False(IsValid(new VivaQuestionInput
        {
            CategoryId = Guid.Empty,
            Prompt = "Who are you?",
        }));
        Assert.False(IsValid(new VivaQuestionInput
        {
            CategoryId = Guid.NewGuid(),
            Prompt = "",
        }));
    }

    [Fact]
    public void MockSessionRequiresUniqueQuestionIds()
    {
        var duplicateId = Guid.NewGuid();
        Assert.False(IsValid(new VivaMockSessionInput
        {
            Title = "Practice",
            QuestionIds = [duplicateId, duplicateId],
        }));
        Assert.True(IsValid(new VivaMockSessionInput
        {
            Title = "Practice",
            QuestionIds = [Guid.NewGuid()],
        }));
    }

    [Fact]
    public void SessionEvaluationRequiresFeedbackAndValidRatings()
    {
        Assert.False(IsValid(new VivaSessionEvaluationInput()));
        Assert.False(IsValid(new VivaSessionEvaluationInput { ConfidenceRating = 6 }));
        Assert.True(IsValid(new VivaSessionEvaluationInput { CommunicationRating = 4 }));
        Assert.True(IsValid(new VivaSessionEvaluationInput { FeedbackNotes = "Good pacing." }));
    }

    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}

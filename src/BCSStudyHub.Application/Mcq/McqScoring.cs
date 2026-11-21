namespace BCSStudyHub.Application.Mcq;

public sealed record McqScoreResult(bool? IsCorrect, decimal MarksAwarded);

public static class McqScoring
{
    public static McqScoreResult Evaluate(
        IReadOnlyCollection<Guid> selectedOptionIds,
        IReadOnlyCollection<Guid> correctOptionIds,
        decimal marksCorrect,
        decimal marksIncorrect,
        decimal marksUnanswered)
    {
        if (selectedOptionIds.Count == 0)
        {
            return new McqScoreResult(null, marksUnanswered);
        }

        var correct = selectedOptionIds.Count == correctOptionIds.Count &&
                      correctOptionIds.ToHashSet().SetEquals(selectedOptionIds);
        return new McqScoreResult(correct, correct ? marksCorrect : marksIncorrect);
    }
}

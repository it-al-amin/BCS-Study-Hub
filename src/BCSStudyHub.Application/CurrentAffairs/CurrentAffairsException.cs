namespace BCSStudyHub.Application.CurrentAffairs;

public sealed class CurrentAffairsException(
    int statusCode,
    string title,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

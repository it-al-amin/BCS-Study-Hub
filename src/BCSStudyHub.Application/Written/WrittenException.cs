namespace BCSStudyHub.Application.Written;

public sealed class WrittenException(
    int statusCode,
    string title,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

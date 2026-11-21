namespace BCSStudyHub.Application.Library;

public sealed class LibraryException(
    int statusCode,
    string title,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

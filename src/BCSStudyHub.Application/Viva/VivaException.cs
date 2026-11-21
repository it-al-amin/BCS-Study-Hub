namespace BCSStudyHub.Application.Viva;

public sealed class VivaException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

namespace BCSStudyHub.Application.Mcq;

public sealed class McqException(int statusCode, string title, string detail)
    : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

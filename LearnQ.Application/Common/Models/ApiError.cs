namespace LearnQ.Application.Common.Models;

public sealed record ApiError(
    string? Code,
    string Message,
    int Status,
    IReadOnlyDictionary<string, string[]> FieldErrors)
{
    public static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors =
        new Dictionary<string, string[]>();

    public ApiError(string? code, string message, int status)
        : this(code, message, status, NoFieldErrors)
    {
    }
}

namespace LearnQ.Application.Common.Models;

public sealed record Result(bool IsSuccess, string? Error = null)
{
    public bool IsFailure => !IsSuccess;
    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
}
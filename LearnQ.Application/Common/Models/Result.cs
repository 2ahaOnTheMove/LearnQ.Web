namespace LearnQ.Application.Common.Models;

public sealed record Result(bool IsSuccess, ApiError? Error = null)
{
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(true);
    public static Result Failure(ApiError error) => new(false, error);
    public static Result Failure(string message) => new(false, new ApiError(null, message, 0));
}
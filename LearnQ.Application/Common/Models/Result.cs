namespace LearnQ.Application.Common.Models;

public sealed record Result(bool IsSuccess, ApiError? Error = null)
{
    public bool IsFailure => !IsSuccess;
    public static Result Success() => new(true);
    public static Result Failure(ApiError error) => new(false, error);
    public static Result Failure(string message) => new(false, new ApiError(null, message, 0));
}

public sealed record Result<T>(bool IsSuccess, T? Value = default, ApiError? Error = null)
{
    public bool IsFailure => !IsSuccess;
    public static Result<T> Success(T value) => new(true, value);
    public static Result<T> Failure(ApiError error) => new(false, default, error);
    public static Result<T> Failure(string message) => new(false, default, new ApiError(null, message, 0));
}
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LearnQ.Application.Common.Models;

namespace LearnQ.Infrastructure.Api.Common;

internal static class ApiErrorReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ApiError> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        var statusCode = (int)response.StatusCode;
        string? code = null;
        string? detail = null;
        IReadOnlyDictionary<string, string[]> fieldErrors = ApiError.NoFieldErrors;

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<BackendProblemDetail>(JsonOptions, ct);
            if (problem is not null)
            {
                code = problem.Code;
                if (!string.IsNullOrWhiteSpace(problem.Detail) && !IsInternalServerErrorDetail(problem.Detail))
                {
                    detail = problem.Detail;
                }

                if (problem.Errors is { Count: > 0 })
                {
                    fieldErrors = MapFieldNamesToPascalCase(problem.Errors);
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Body was not valid JSON or could not be deserialized as ProblemDetail
        }

        var message = !string.IsNullOrWhiteSpace(detail)
            ? detail
            : GetFallbackMessage(response.StatusCode);

        return new ApiError(code, message, statusCode, fieldErrors);
    }

    public static ApiError FromException(Exception ex)
    {
        return ex switch
        {
            TaskCanceledException or TimeoutException =>
                new ApiError(null, "The request timed out. Please try again.", (int)HttpStatusCode.RequestTimeout),
            HttpRequestException =>
                new ApiError(null, "Can't reach the server. Please check your connection and try again.", (int)HttpStatusCode.ServiceUnavailable),
            _ =>
                new ApiError(null, "An unexpected network error occurred. Please try again.", (int)HttpStatusCode.InternalServerError)
        };
    }

    private static IReadOnlyDictionary<string, string[]> MapFieldNamesToPascalCase(
        Dictionary<string, string[]> rawErrors)
    {
        var mapped = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, messages) in rawErrors)
        {
            var normalizedKey = ToPascalCase(key);
            mapped[normalizedKey] = messages;
        }
        return mapped;
    }

    private static string ToPascalCase(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return fieldName;
        if (fieldName.Length == 1) return char.ToUpperInvariant(fieldName[0]).ToString();
        return char.ToUpperInvariant(fieldName[0]) + fieldName[1..];
    }

    private static bool IsInternalServerErrorDetail(string detail) =>
        detail.Contains("Exception:", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("org.springframework", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("java.lang", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("at line", StringComparison.OrdinalIgnoreCase) ||
        detail.Contains("\tat ", StringComparison.OrdinalIgnoreCase);

    private static string GetFallbackMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Some of the details you entered are invalid.",
        HttpStatusCode.Unauthorized => "You must be signed in to perform this action.",
        HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
        HttpStatusCode.NotFound => "The requested resource was not found.",
        HttpStatusCode.Conflict => "An account or resource with those details already exists.",
        HttpStatusCode.UnprocessableEntity => "The request could not be processed.",
        >= HttpStatusCode.InternalServerError => "An unexpected server error occurred. Please try again later.",
        _ => "Something went wrong. Please try again."
    };

    private sealed record BackendProblemDetail(
        int? Status,
        string? Title,
        string? Detail,
        string? Code,
        Dictionary<string, string[]>? Errors);
}

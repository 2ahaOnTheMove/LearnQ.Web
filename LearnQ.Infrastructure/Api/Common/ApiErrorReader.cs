using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LearnQ.Infrastructure.Api.Common;

internal static class ApiErrorReader
{
    public static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Message)) return problem.Message;
        }
        catch (JsonException)
        {
            // body wasn't JSON
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict => "An account with those details already exists.",
            HttpStatusCode.BadRequest => "Some of the details you entered are invalid.",
            _ => "Something went wrong. Please try again."
        };
    }

    private sealed record ProblemBody(string? Detail, string? Message);
}

using System.Net.Http.Json;
using System.Text.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;

namespace LearnQ.Infrastructure.Api;

internal sealed class ClientApi(HttpClient http) : IClientApi
{
    public async Task<Result> RegisterAsync(RegisterClientRequest request, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("api/clients/register", request, ct);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            return Result.Failure(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return Result.Failure("Can't reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return Result.Failure("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Message)) return problem.Message;
        }
        catch (JsonException) { /* body wasn't JSON */ }

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Conflict => "An account with those details already exists.",
            System.Net.HttpStatusCode.BadRequest => "Some of the details you entered are invalid.",
            _ => "Something went wrong. Please try again."
        };
    }

    // Covers Spring's ProblemDetail ("detail") and a typical custom body ("message")
    private sealed record ProblemBody(string? Detail, string? Message);
}
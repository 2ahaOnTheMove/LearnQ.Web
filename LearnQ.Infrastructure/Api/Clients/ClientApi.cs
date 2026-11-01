using System.Net.Http.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using LearnQ.Infrastructure.Api.Common;

namespace LearnQ.Infrastructure.Api.Clients;

internal sealed class ClientApi(HttpClient http) : IClientApi
{
    public async Task<Result> RegisterAsync(RegisterClientRequest request, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("api/clients/register", request, ct);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            return Result.Failure(await ApiErrorReader.ReadErrorAsync(response, ct));
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
}

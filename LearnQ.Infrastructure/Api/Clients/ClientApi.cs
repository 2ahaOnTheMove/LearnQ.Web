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
            using var response = await http.PostAsJsonAsync("api/clients/register", request, JsonSerializerDefaults.Options, ct);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            var error = await ApiErrorReader.ReadErrorAsync(response, ct);
            return Result.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result.Failure(ApiErrorReader.FromException(ex));
        }
    }
}

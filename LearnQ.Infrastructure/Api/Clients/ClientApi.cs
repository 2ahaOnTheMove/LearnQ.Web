using System.Net.Http.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using LearnQ.Infrastructure.Api.Common;

namespace LearnQ.Infrastructure.Api.Clients;

internal sealed class ClientApi(HttpClient http) : IClientApi
{
    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterClientRequest request, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(ClientEndpoints.Register, request, JsonSerializerDefaults.Options, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<RegisterResponse>(JsonSerializerDefaults.Options, ct);
                return body is not null
                    ? Result<RegisterResponse>.Success(body)
                    : Result<RegisterResponse>.Failure(new ApiError(null, "Invalid registration response.", 500));
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, ct);
            return Result<RegisterResponse>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<RegisterResponse>.Failure(ApiErrorReader.FromException(ex));
        }
    }
}

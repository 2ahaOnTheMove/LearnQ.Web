using System.Net.Http.Headers;
using System.Net.Http.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Infrastructure.Api.Common;
using LearnQ.Infrastructure.Api.Onboarding;

namespace LearnQ.Infrastructure.Api.Avatars;

internal sealed class AvatarApi(HttpClient http) : IAvatarApi
{
    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await http.PostAsync(OnboardingEndpoints.AvatarUpload, content, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AvatarUploadResponse>(JsonSerializerDefaults.Options, ct);
                if (!string.IsNullOrWhiteSpace(result?.Url))
                {
                    return Result<string>.Success(result.Url);
                }
                return Result<string>.Failure("Upload succeeded but no avatar URL was returned.");
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, ct);
            return Result<string>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<string>.Failure(ApiErrorReader.FromException(ex));
        }
    }

    private sealed record AvatarUploadResponse(string? Url);
}

using System.Net.Http.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using LearnQ.Infrastructure.Api.Common;

namespace LearnQ.Infrastructure.Api.Auth;

/// <summary>
/// Anonymous auth API client for login, refresh, and logout.
/// Must NOT use BearerTokenHandler to avoid recursion during token refresh.
/// </summary>
internal sealed class AuthApi(HttpClient httpClient) : IAuthApi
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                AuthEndpoints.Login,
                request,
                JsonSerializerDefaults.Options,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<LoginResponse>(
                    JsonSerializerDefaults.Options,
                    cancellationToken);

                return body is not null
                    ? Result<LoginResponse>.Success(body)
                    : Result<LoginResponse>.Failure(new ApiError(null, "Invalid login response from server.", 500));
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, cancellationToken);
            return Result<LoginResponse>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<LoginResponse>.Failure(ApiErrorReader.FromException(ex));
        }
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                AuthEndpoints.Refresh,
                request,
                JsonSerializerDefaults.Options,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<LoginResponse>(
                    JsonSerializerDefaults.Options,
                    cancellationToken);

                return body is not null
                    ? Result<LoginResponse>.Success(body)
                    : Result<LoginResponse>.Failure(new ApiError(null, "Invalid refresh token response from server.", 500));
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, cancellationToken);
            return Result<LoginResponse>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<LoginResponse>.Failure(ApiErrorReader.FromException(ex));
        }
    }

    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                AuthEndpoints.Logout,
                request,
                JsonSerializerDefaults.Options,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, cancellationToken);
            return Result.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result.Failure(ApiErrorReader.FromException(ex));
        }
    }
}

/// <summary>
/// Authenticated auth API client for operations requiring a bearer token.
/// Used with BearerTokenHandler for automatic token attachment and refresh.
/// </summary>
internal sealed class AuthenticatedAuthApi(HttpClient httpClient) : IAuthenticatedAuthApi
{
    public async Task<Result<CurrentUser>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(AuthEndpoints.Me, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<CurrentUser>(
                    JsonSerializerDefaults.Options,
                    cancellationToken);

                return body is not null
                    ? Result<CurrentUser>.Success(body)
                    : Result<CurrentUser>.Failure(new ApiError(null, "Invalid user profile response from server.", 500));
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, cancellationToken);
            return Result<CurrentUser>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<CurrentUser>.Failure(ApiErrorReader.FromException(ex));
        }
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                AuthEndpoints.VerifyEmail,
                request,
                JsonSerializerDefaults.Options,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, cancellationToken);
            return Result.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result.Failure(ApiErrorReader.FromException(ex));
        }
    }

    public async Task<Result<ResendVerificationResponse>> ResendVerificationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                AuthEndpoints.ResendVerification,
                content: null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ResendVerificationResponse>(
                    JsonSerializerDefaults.Options,
                    cancellationToken);

                return Result<ResendVerificationResponse>.Success(body ?? new ResendVerificationResponse(60));
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, cancellationToken);
            return Result<ResendVerificationResponse>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<ResendVerificationResponse>.Failure(ApiErrorReader.FromException(ex));
        }
    }
}

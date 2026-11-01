using System.Net.Http.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Onboarding;
using LearnQ.Infrastructure.Api.Common;

namespace LearnQ.Infrastructure.Api.Onboarding;

internal sealed class OnboardingApi(HttpClient http) : IOnboardingApi
{
    public async Task<Result<OnboardingStatusResponse>> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(OnboardingEndpoints.Status, ct);
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<OnboardingStatusResponse>(JsonSerializerDefaults.Options, ct);
                return Result<OnboardingStatusResponse>.Success(status ?? new OnboardingStatusResponse(null, false));
            }

            var error = await ApiErrorReader.ReadErrorAsync(response, ct);
            return Result<OnboardingStatusResponse>.Failure(error);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Result<OnboardingStatusResponse>.Failure(ApiErrorReader.FromException(ex));
        }
    }

    public async Task<Result> SelectRoleAsync(SelectRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync(OnboardingEndpoints.SelectRole, request, JsonSerializerDefaults.Options, ct);
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

    public async Task<Result> SubmitStudentAsync(StudentOnboardingRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync(OnboardingEndpoints.Student, request, JsonSerializerDefaults.Options, ct);
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

    public async Task<Result> SubmitTeacherAsync(TeacherOnboardingRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync(OnboardingEndpoints.Teacher, request, JsonSerializerDefaults.Options, ct);
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

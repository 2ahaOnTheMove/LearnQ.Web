using LearnQ.Application.Common.Enums;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;

namespace LearnQ.Infrastructure.Api.Auth;

internal sealed class StubAuthApi : IAuthApi
{
    private static string _id = "usr_dev_1";
    private static string _email = "jane.doe@example.com";
    private static string _displayName = "Jane Doe";
    private static UserRole? _role;
    private static bool _emailVerified;
    private static bool _onboardingCompleted;
    private static string? _onboardingStatus;

    public static void SetRole(UserRole? role)
    {
        _role = role;
    }

    public static void SetOnboardingCompleted(bool completed, UserRole role)
    {
        _role = role;
        _onboardingCompleted = completed;
        _onboardingStatus = completed ? "COMPLETED" : "IN_PROGRESS";
    }

    public static void ResetState(string email, string displayName)
    {
        _id = "usr_" + Guid.NewGuid().ToString("N")[..8];
        _email = email;
        _displayName = displayName;
        _role = null;
        _emailVerified = false;
        _onboardingCompleted = false;
        _onboardingStatus = null;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponse>.Failure(new ApiError("UNAUTHORIZED", "Invalid email/username or password", 401));
        }

        if (!string.Equals(_email, request.Identifier, StringComparison.OrdinalIgnoreCase))
        {
            _email = request.Identifier;
            _displayName = request.Identifier.Contains('@') ? request.Identifier.Split('@')[0] : request.Identifier;
            _role = null;
            _emailVerified = false;
            _onboardingCompleted = false;
            _onboardingStatus = null;
        }

        var expiry = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(15);
        return Result<LoginResponse>.Success(new LoginResponse("stub-access-token", "stub-refresh-token", expiry));
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return Result<LoginResponse>.Success(new LoginResponse("stub-access-token-refreshed", "stub-refresh-token-refreshed", DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<CurrentUser>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return Result<CurrentUser>.Success(new CurrentUser(_id, _email, _displayName, _role, _emailVerified, _onboardingCompleted, _onboardingStatus));
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        if (request.Code == "123456")
        {
            _emailVerified = true;
            return Result.Success();
        }

        return Result.Failure(new ApiError("VALIDATION_FAILED", "Invalid verification code. Enter 123456 to verify.", 400));
    }

    public async Task<Result<ResendVerificationResponse>> ResendVerificationAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return Result<ResendVerificationResponse>.Success(new ResendVerificationResponse(60));
    }
}

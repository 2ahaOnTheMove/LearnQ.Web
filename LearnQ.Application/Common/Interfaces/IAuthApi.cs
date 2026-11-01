using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;

namespace LearnQ.Application.Common.Interfaces;

public interface IAuthApi
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result<CurrentUser>> GetMeAsync(CancellationToken cancellationToken = default);
    Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<Result<ResendVerificationResponse>> ResendVerificationAsync(CancellationToken cancellationToken = default);
}

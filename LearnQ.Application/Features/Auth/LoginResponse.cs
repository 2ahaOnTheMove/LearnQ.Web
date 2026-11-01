namespace LearnQ.Application.Features.Auth;

public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

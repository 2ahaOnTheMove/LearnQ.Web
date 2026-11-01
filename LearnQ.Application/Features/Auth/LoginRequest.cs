namespace LearnQ.Application.Features.Auth;

public sealed record LoginRequest(string Identifier, string Password, bool RememberMe = false);

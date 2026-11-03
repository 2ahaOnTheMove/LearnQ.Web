namespace LearnQ.Application.Features.Auth.Session;

/// <summary>
/// AsyncLocal context that carries the session ID from AuthService to BearerTokenHandler.
/// This bridges the DI scope gap: IHttpClientFactory creates its own scope for DelegatingHandlers,
/// so SessionAccessor cannot resolve the Blazor circuit's AuthenticationStateProvider.
/// AsyncLocal flows across scope boundaries within the same logical call chain.
/// </summary>
public static class SessionContext
{
    private static readonly AsyncLocal<string?> CurrentSessionId = new();

    public static string? SessionId
    {
        get => CurrentSessionId.Value;
        set => CurrentSessionId.Value = value;
    }
}

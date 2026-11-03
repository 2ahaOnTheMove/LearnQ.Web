using LearnQ.Application.Common.Constants;
using LearnQ.Application.Features.Auth.Session;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace LearnQ.Presentation.Authentication;

public sealed class RevalidatingAuthStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RevalidatingAuthStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
    }

    // Revalidate every 15 minutes. This is a balance between:
    // - Detecting expired backend sessions in a reasonable time.
    // - Not overwhelming the backend with validation requests.
    // The BearerTokenHandler already handles per-request token validation,
    // so this is a secondary safety net for the Blazor circuit.
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(15);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var principal = authenticationState.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var sid = principal.FindFirst(AuthClaimTypes.Sid)?.Value;
        if (string.IsNullOrEmpty(sid))
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var sessionStore = scope.ServiceProvider.GetRequiredService<ISessionTokenStore>();
        var tokens = await sessionStore.GetTokensAsync(sid, cancellationToken);

        return tokens is not null;
    }
}

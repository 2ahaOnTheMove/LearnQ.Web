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

    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(30);

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

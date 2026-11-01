using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearnQ.Presentation.Security;

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

        var sid = principal.FindFirst(AppClaims.Sid)?.Value;
        if (string.IsNullOrEmpty(sid))
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var sessionStore = scope.ServiceProvider.GetRequiredService<IRedisSessionStore>();
        var tokens = await sessionStore.GetTokensAsync(sid, cancellationToken);

        return tokens is not null;
    }
}

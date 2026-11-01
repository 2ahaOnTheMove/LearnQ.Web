using LearnQ.Application.Common.Constants;
using LearnQ.Application.Features.Auth.Session;
using Microsoft.AspNetCore.Components.Authorization;

namespace LearnQ.Presentation.Authentication;

public sealed class SessionAccessor : ISessionAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authStateProvider;

    public SessionAccessor(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
    }

    public async Task<string?> GetSessionIdAsync(CancellationToken cancellationToken = default)
    {
        // HttpContext is available during normal SSR requests
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var sid = httpContext.User.FindFirst(AuthClaimTypes.Sid)?.Value;
            if (!string.IsNullOrEmpty(sid))
            {
                return sid;
            }
        }

        // In interactive circuits HttpContext is null; resolve sid from AuthenticationStateProvider
        try
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            return state.User.FindFirst(AuthClaimTypes.Sid)?.Value;
        }
        catch
        {
            // Circuit may not have initialized state yet
        }

        return null;
    }
}

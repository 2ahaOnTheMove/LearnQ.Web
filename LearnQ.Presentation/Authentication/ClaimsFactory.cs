using System.Security.Claims;
using LearnQ.Application.Common.Constants;
using LearnQ.Application.Common.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LearnQ.Presentation.Authentication;

public static class ClaimsFactory
{
    public static ClaimsPrincipal CreatePrincipal(CurrentUser me, string sid, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(AuthClaimTypes.Sub, me.Id),
            new(AuthClaimTypes.Name, me.DisplayName),
            new(AuthClaimTypes.Email, me.Email),
            new(AuthClaimTypes.EmailVerified, me.EmailVerified ? "true" : "false"),
            new(AuthClaimTypes.OnboardingCompleted, me.OnboardingCompleted ? "true" : "false"),
            new(AuthClaimTypes.Sid, sid),
            new(AuthClaimTypes.IsPersistent, rememberMe ? "true" : "false")
        };

        if (me.Role.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, me.Role.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(me.OnboardingStatus))
        {
            claims.Add(new Claim(AuthClaimTypes.OnboardingStatus, me.OnboardingStatus));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}

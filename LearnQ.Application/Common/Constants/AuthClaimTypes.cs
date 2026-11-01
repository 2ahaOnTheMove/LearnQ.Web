using System.Security.Claims;

namespace LearnQ.Application.Common.Constants;

public static class AuthClaimTypes
{
    // Standard claim type aliases
    public const string Sub = ClaimTypes.NameIdentifier;
    public const string Name = ClaimTypes.Name;
    public const string Email = ClaimTypes.Email;
    public const string Role = ClaimTypes.Role;

    // Custom claim types
    public const string Sid = "sid";
    public const string EmailVerified = "email_verified";
    public const string OnboardingCompleted = "onboarding_completed";
    public const string OnboardingStatus = "onboarding_status";
    public const string IsPersistent = "is_persistent";
}

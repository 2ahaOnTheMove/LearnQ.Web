using System.Security.Claims;

namespace LearnQ.Presentation.Security;

public static class AppClaims
{
    public const string Sub = ClaimTypes.NameIdentifier;
    public const string Name = ClaimTypes.Name;
    public const string Email = ClaimTypes.Email;
    public const string Role = ClaimTypes.Role;
    public const string EmailVerified = "email_verified";
    public const string OnboardingCompleted = "onboarding_completed";
    public const string OnboardingStatus = "onboarding_status";
    public const string Sid = "sid";
}

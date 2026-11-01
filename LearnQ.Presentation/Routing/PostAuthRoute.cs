using System.Security.Claims;
using LearnQ.Application.Common.Enums;
using LearnQ.Application.Common.Models;
using LearnQ.Presentation.Security;

namespace LearnQ.Presentation.Routing;

public static class PostAuthRoute
{
    public static string GetPostAuthDestination(ClaimsPrincipal? principal, string? returnUrl = null)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return AppRoutes.Login;
        }

        var isVerified = principal.FindFirst(AppClaims.EmailVerified)?.Value == "true";
        if (!isVerified)
        {
            return AppRoutes.VerifyEmail;
        }

        var roleStr = principal.FindFirst(ClaimTypes.Role)?.Value;
        UserRole? role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : null;

        if (role is null)
        {
            return AppRoutes.OnboardingRole;
        }

        var isOnboarded = principal.FindFirst(AppClaims.OnboardingCompleted)?.Value == "true";
        if (!isOnboarded)
        {
            return role == UserRole.Student
                ? AppRoutes.OnboardingStudent
                : AppRoutes.OnboardingTeacher;
        }

        if (role == UserRole.Teacher)
        {
            var status = principal.FindFirst(AppClaims.OnboardingStatus)?.Value;
            if (string.Equals(status, "REVIEW", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "PENDING_REVIEW", StringComparison.OrdinalIgnoreCase))
            {
                return AppRoutes.OnboardingStatus;
            }
        }

        var safeReturn = ReturnUrlValidator.GetSafeReturnUrl(returnUrl);
        if (!string.IsNullOrEmpty(safeReturn) && safeReturn != AppRoutes.Home && !safeReturn.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            return safeReturn;
        }

        return AppRoutes.Dashboard;
    }

    public static string GetPostAuthDestination(CurrentUser user, string? returnUrl = null)
    {
        if (!user.EmailVerified)
        {
            return AppRoutes.VerifyEmail;
        }

        if (user.Role is null)
        {
            return AppRoutes.OnboardingRole;
        }

        if (!user.OnboardingCompleted)
        {
            return user.Role == UserRole.Student
                ? AppRoutes.OnboardingStudent
                : AppRoutes.OnboardingTeacher;
        }

        if (user.Role == UserRole.Teacher &&
            (string.Equals(user.OnboardingStatus, "REVIEW", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(user.OnboardingStatus, "PENDING_REVIEW", StringComparison.OrdinalIgnoreCase)))
        {
            return AppRoutes.OnboardingStatus;
        }

        var safeReturn = ReturnUrlValidator.GetSafeReturnUrl(returnUrl);
        if (!string.IsNullOrEmpty(safeReturn) && safeReturn != AppRoutes.Home && !safeReturn.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            return safeReturn;
        }

        return AppRoutes.Dashboard;
    }
}

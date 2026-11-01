using LearnQ.Application.Common.Enums;
using LearnQ.Application.Features.Onboarding;
using Microsoft.AspNetCore.Http;

namespace LearnQ.Presentation.Routing;

public static class OnboardingRoute
{
    public static bool ShouldSkipGuard(HttpContext? httpContext) =>
        httpContext is not null && HttpMethods.IsPost(httpContext.Request.Method);

    public static string GetDestination(OnboardingStatusResponse status)
    {
        if (status.HasCompletedProfile)
        {
            return status.Role == UserRole.Teacher
                ? AppRoutes.OnboardingStatus
                : AppRoutes.Dashboard;
        }

        return status.Role switch
        {
            UserRole.Student => AppRoutes.OnboardingStudent,
            UserRole.Teacher => AppRoutes.OnboardingTeacher,
            _ => AppRoutes.OnboardingRole
        };
    }

    public static string? GetGuardRedirect(OnboardingStatusResponse status, string currentPath)
    {
        if (status.HasCompletedProfile)
        {
            return status.Role == UserRole.Teacher
                ? AppRoutes.OnboardingStatus
                : AppRoutes.Dashboard;
        }

        if (status.Role is null && !string.Equals(currentPath, AppRoutes.OnboardingRole, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoutes.OnboardingRole;
        }

        if (status.Role == UserRole.Student && string.Equals(currentPath, AppRoutes.OnboardingTeacher, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoutes.OnboardingStudent;
        }

        if (status.Role == UserRole.Teacher && string.Equals(currentPath, AppRoutes.OnboardingStudent, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoutes.OnboardingTeacher;
        }

        return null;
    }
}

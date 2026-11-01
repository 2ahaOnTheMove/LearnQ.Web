using LearnQ.Application.Common.Enums;

namespace LearnQ.Application.Features.Onboarding;

public sealed record OnboardingStatusResponse(
    UserRole? Role,
    bool HasCompletedProfile
);

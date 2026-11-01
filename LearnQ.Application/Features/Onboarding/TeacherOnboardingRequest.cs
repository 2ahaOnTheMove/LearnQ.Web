using LearnQ.Application.Common.Enums;

namespace LearnQ.Application.Features.Onboarding;

public sealed record TeacherOnboardingRequest(
    string? AvatarUrl,
    string Headline,
    string Bio,
    string? IntroVideoUrl,
    Gender Gender,
    int YearsExperience,
    string Language,
    bool TeachesKids,
    bool TeachesTeens,
    bool TeachesAdults,
    GenderRestriction GenderRestriction,
    int MaxWeeklyHours
);

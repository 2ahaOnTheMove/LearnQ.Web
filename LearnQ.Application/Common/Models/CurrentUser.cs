using LearnQ.Application.Common.Enums;

namespace LearnQ.Application.Common.Models;

public sealed record CurrentUser(
    string Id,
    string Email,
    string DisplayName,
    UserRole? Role,
    bool EmailVerified,
    bool OnboardingCompleted,
    string? OnboardingStatus,
    long? ResendAvailableAt = null);

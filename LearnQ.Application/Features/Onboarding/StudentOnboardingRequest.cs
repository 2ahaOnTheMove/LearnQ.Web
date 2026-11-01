namespace LearnQ.Application.Features.Onboarding;

public sealed record StudentOnboardingRequest(
    bool ForSelf,
    bool GuardianConfirmed,
    IReadOnlyList<LearnerRequest> Learners
);

using LearnQ.Application.Common.Enums;

namespace LearnQ.Application.Features.Onboarding;

public sealed record LearnerRequest(
    string? AvatarUrl,
    string? GivenName,
    string? FamilyName,
    DateOnly? DateOfBirth,
    Gender Gender,
    GenderRestriction PreferredTeacherGender,
    string? PreviousExperience,
    string? NotesForTeacher,
    bool RecordingConsent,
    string? AdditionalNeeds
);

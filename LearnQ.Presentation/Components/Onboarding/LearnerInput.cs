using System.ComponentModel.DataAnnotations;
using LearnQ.Application.Common.Constants;
using LearnQ.Application.Common.Enums;

namespace LearnQ.Presentation.Components.Onboarding;

public sealed class LearnerInput : IValidatableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? AvatarUrl { get; set; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Given name must be between 1 and 100 characters.")]
    public string? GivenName { get; set; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Family name must be between 1 and 100 characters.")]
    public string? FamilyName { get; set; }

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    public Gender Gender { get; set; } = Gender.PreferNotToSay;

    public GenderRestriction PreferredTeacherGender { get; set; } = GenderRestriction.None;

    [StringLength(500, ErrorMessage = "Previous experience cannot exceed 500 characters.")]
    public string? PreviousExperience { get; set; }

    [StringLength(1000, ErrorMessage = "Notes for teacher cannot exceed 1000 characters.")]
    public string? NotesForTeacher { get; set; }

    public bool RecordingConsent { get; set; }

    [StringLength(1000, ErrorMessage = "Additional needs cannot exceed 1000 characters.")]
    public string? AdditionalNeeds { get; set; }

    public bool RequiresNames { get; set; }
    public bool EnforceAccountHolderMinAge { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RequiresNames)
        {
            if (string.IsNullOrWhiteSpace(GivenName))
            {
                yield return new ValidationResult("Given name is required.", [nameof(GivenName)]);
            }

            if (string.IsNullOrWhiteSpace(FamilyName))
            {
                yield return new ValidationResult("Family name is required.", [nameof(FamilyName)]);
            }
        }

        if (DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (DateOfBirth.Value > today)
            {
                yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
            }
            else if (DateOfBirth.Value < today.AddYears(-100))
            {
                yield return new ValidationResult("Date of birth cannot be more than 100 years ago.", [nameof(DateOfBirth)]);
            }
            else if (EnforceAccountHolderMinAge)
            {
                var minBirthDate = today.AddYears(-OnboardingConstants.MinAccountHolderAge);
                if (DateOfBirth.Value > minBirthDate)
                {
                    yield return new ValidationResult(
                        "You must be at least 16 to create an account. Ask a parent or guardian to set it up for you.",
                        [nameof(DateOfBirth)]);
                }
            }
        }
    }

    public LearnerInput Clone()
    {
        return new LearnerInput
        {
            Id = Id,
            AvatarUrl = AvatarUrl,
            GivenName = GivenName,
            FamilyName = FamilyName,
            DateOfBirth = DateOfBirth,
            Gender = Gender,
            PreferredTeacherGender = PreferredTeacherGender,
            PreviousExperience = PreviousExperience,
            NotesForTeacher = NotesForTeacher,
            RecordingConsent = RecordingConsent,
            AdditionalNeeds = AdditionalNeeds,
            RequiresNames = RequiresNames,
            EnforceAccountHolderMinAge = EnforceAccountHolderMinAge
        };
    }

    public bool HasData() =>
        !string.IsNullOrWhiteSpace(AvatarUrl) ||
        !string.IsNullOrWhiteSpace(GivenName) ||
        !string.IsNullOrWhiteSpace(FamilyName) ||
        DateOfBirth.HasValue ||
        !string.IsNullOrWhiteSpace(PreviousExperience) ||
        !string.IsNullOrWhiteSpace(NotesForTeacher) ||
        !string.IsNullOrWhiteSpace(AdditionalNeeds) ||
        RecordingConsent;
}

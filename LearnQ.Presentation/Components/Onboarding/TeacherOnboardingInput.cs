using System.ComponentModel.DataAnnotations;
using LearnQ.Application.Common.Enums;

namespace LearnQ.Presentation.Components.Onboarding;

public sealed class TeacherOnboardingInput : IValidatableObject
{
    public string? AvatarUrl { get; set; }

    [Required(ErrorMessage = "Professional headline is required.")]
    [StringLength(100, ErrorMessage = "Headline cannot exceed 100 characters.")]
    public string Headline { get; set; } = "";

    [Required(ErrorMessage = "Bio is required.")]
    [StringLength(2000, MinimumLength = 50, ErrorMessage = "Bio must be between 50 and 2000 characters.")]
    public string Bio { get; set; } = "";

    [Url(ErrorMessage = "Introduction video URL must be a valid URL.")]
    [RegularExpression(@"^https://.+", ErrorMessage = "Introduction video URL must be a valid absolute https URL.")]
    public string? IntroVideoUrl { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    public Gender Gender { get; set; } = Gender.PreferNotToSay;

    [Required(ErrorMessage = "Years of experience is required.")]
    [Range(0, 60, ErrorMessage = "Years of experience must be between 0 and 60.")]
    public int? YearsExperience { get; set; } = 0;

    [Required(ErrorMessage = "Teaching language is required.")]
    [RegularExpression(@"^[a-z]{2}$", ErrorMessage = "Language must be a valid 2-letter lowercase code.")]
    public string Language { get; set; } = "en";

    public bool TeachesKids { get; set; }
    public bool TeachesTeens { get; set; }
    public bool TeachesAdults { get; set; }

    [Required(ErrorMessage = "Student gender restriction is required.")]
    public GenderRestriction GenderRestriction { get; set; } = GenderRestriction.None;

    [Required(ErrorMessage = "Maximum weekly hours is required.")]
    [Range(1, 80, ErrorMessage = "Maximum weekly hours must be between 1 and 80.")]
    public int? MaxWeeklyHours { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TeachesKids && !TeachesTeens && !TeachesAdults)
        {
            yield return new ValidationResult(
                "Please select at least one age group you teach (Kids, Teens, or Adults).",
                [nameof(TeachesKids)]);
        }
    }
}

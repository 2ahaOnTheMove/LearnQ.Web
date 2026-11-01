using System.ComponentModel.DataAnnotations;

namespace LearnQ.Presentation.Components.Auth;

public sealed class RegisterInput
{
    [Required(ErrorMessage = "Given name is required to continue."),
     StringLength(100, ErrorMessage = "Given name is too long.")]
    public string GivenName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Family name is required to continue."),
     StringLength(100, ErrorMessage = "Family name is too long.")]
    public string FamilyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A display name is required for your profile."),
     StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Letters, numbers and dashes only.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A valid email address is required."),
     EmailAddress(ErrorMessage = "Please check your email format (e.g., name@example.com).")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A secure password is required."),
     StringLength(100, MinimumLength = 8, ErrorMessage = "Passwords must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password."),
     Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string ReferralCode { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions.")]
    public bool TermsAccepted { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the privacy policy.")]
    public bool PrivacyAccepted { get; set; }
}

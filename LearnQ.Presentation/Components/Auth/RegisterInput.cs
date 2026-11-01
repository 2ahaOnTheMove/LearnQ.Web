using System.ComponentModel.DataAnnotations;

namespace LearnQ.Presentation.Components.Auth;

public sealed class RegisterInput
{
    [Required, StringLength(50)]
    public string GivenName { get; set; } = "";

    [Required, StringLength(50)]
    public string FamilyName { get; set; } = "";

    [Required, StringLength(30, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Letters, numbers, dots and underscores only.")]
    public string DisplayName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
    
    [Required, StringLength(20, MinimumLength = 20)]
    public string ReferralCode { get; set; } = "";

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms.")]
    public bool TermsAccepted { get; set; }
    
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the privacy policy.")]
    public bool PrivacyAccepted { get; set; }
}
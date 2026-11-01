using System.ComponentModel.DataAnnotations;

namespace LearnQ.Presentation.Components.Auth;

public sealed class VerifyEmailInput
{
    [Required(ErrorMessage = "Verification code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit verification code.")]
    public string Code { get; set; } = string.Empty;
}

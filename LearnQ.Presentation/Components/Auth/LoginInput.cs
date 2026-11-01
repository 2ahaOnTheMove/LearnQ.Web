using System.ComponentModel.DataAnnotations;

namespace LearnQ.Presentation.Components.Auth;

public sealed class LoginInput
{
    [Required(ErrorMessage = "Email or username is required.")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

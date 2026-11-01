using System.ComponentModel.DataAnnotations;
using LearnQ.Application.Common.Enums;

namespace LearnQ.Presentation.Components.Onboarding;

public sealed class RoleSelectionInput
{
    [Required(ErrorMessage = "Choose a role to continue.")]
    public UserRole? Role { get; set; }
}

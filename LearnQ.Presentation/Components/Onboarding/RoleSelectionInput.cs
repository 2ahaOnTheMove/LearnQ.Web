using System.ComponentModel.DataAnnotations;
using LearnQ.Application.Common.Enums;

namespace LearnQ.Presentation.Components.Onboarding;

public sealed class RoleSelectionInput
{
    [Required(ErrorMessage = "Please select whether you want to join as a Student or Teacher.")]
    public UserRole? Role { get; set; }
}

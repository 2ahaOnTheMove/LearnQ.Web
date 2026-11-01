using Microsoft.AspNetCore.Components.Forms;

namespace LearnQ.Presentation.Components.Shared;

public sealed class BootstrapFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier) =>
        editContext.GetValidationMessages(fieldIdentifier).Any() ? "is-invalid" : "";
}
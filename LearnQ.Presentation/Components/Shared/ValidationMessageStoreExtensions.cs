using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace LearnQ.Presentation.Components.Shared;

public static class ValidationMessageStoreExtensions
{
    public static void AddFieldErrors(
        this ValidationMessageStore messageStore,
        object model,
        IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        ArgumentNullException.ThrowIfNull(messageStore);
        ArgumentNullException.ThrowIfNull(model);

        if (fieldErrors is null || fieldErrors.Count == 0) return;

        var modelType = model.GetType();

        foreach (var (fieldName, messages) in fieldErrors)
        {
            if (messages is null || messages.Length == 0) continue;

            var prop = modelType.GetProperty(
                fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            var propertyName = prop?.Name ?? fieldName;
            var fieldIdentifier = new FieldIdentifier(model, propertyName);

            messageStore.Add(fieldIdentifier, messages);
        }
    }
}

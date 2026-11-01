using System.Globalization;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;

namespace LearnQ.Infrastructure.Api.Languages;

internal sealed class LanguageCatalog : ILanguageCatalog
{
    private static readonly IReadOnlyList<LanguageOption> CachedLanguages = LoadLanguages();

    public IReadOnlyList<LanguageOption> GetLanguages() => CachedLanguages;

    private static IReadOnlyList<LanguageOption> LoadLanguages()
    {
        try
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            var languages = cultures
                .Where(c => !string.IsNullOrEmpty(c.TwoLetterISOLanguageName) && c.TwoLetterISOLanguageName.Length == 2)
                .GroupBy(c => c.TwoLetterISOLanguageName.ToLowerInvariant())
                .Select(g => new LanguageOption(g.Key, g.First().DisplayName))
                .OrderBy(l => l.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (languages.Count > 0)
            {
                return languages;
            }
        }
        catch
        {
            // Fallback if culture information is unavailable
        }

        return FallbackLanguages();
    }

    private static IReadOnlyList<LanguageOption> FallbackLanguages() =>
    [
        new("ar", "Arabic"),
        new("de", "German"),
        new("en", "English"),
        new("es", "Spanish"),
        new("fr", "French"),
        new("hi", "Hindi"),
        new("it", "Italian"),
        new("ja", "Japanese"),
        new("ko", "Korean"),
        new("pt", "Portuguese"),
        new("ru", "Russian"),
        new("tr", "Turkish"),
        new("zh", "Chinese")
    ];
}

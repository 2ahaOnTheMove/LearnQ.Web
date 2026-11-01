using LearnQ.Application.Common.Models;

namespace LearnQ.Application.Common.Interfaces;

public interface ILanguageCatalog
{
    IReadOnlyList<LanguageOption> GetLanguages();
}

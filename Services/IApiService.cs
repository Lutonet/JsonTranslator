using JsonTranslator.Models;

namespace JsonTranslator.Services
{
    public interface IApiService
    {
        Task<List<Language>> GetLanguages();

        Task<List<TranslationBulk>> Translate(List<Translation> phrases, string sourceLanguage);
    }
}
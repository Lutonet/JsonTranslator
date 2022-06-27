using JsonTranslator.Models;

namespace JsonTranslator.Services
{
    public interface IApiService
    {
        public void Init();

        Task<List<Language>> GetLanguages();

        Task<List<TranslationBulk>> Translate(List<Translation> phrases, CancellationToken token);

        Task<bool> TestServer();
    }
}
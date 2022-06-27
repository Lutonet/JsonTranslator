using JsonTranslator.Models;

namespace JsonTranslator.Services
{
    public interface ISourcesService
    {
        Task<List<TranslationBulk>> GetAllNeededTranslations();
        Task<TranslationWorkload> GetWorkload();
        Task Init();
        Task StoreResults(List<TranslationBulk> toStore);
    }
}
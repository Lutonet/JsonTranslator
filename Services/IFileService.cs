namespace JsonTranslator.Services
{
    public interface IFileService
    {
        Task<bool> CheckIfDefaultExists(string folder, string defaultLanguage);
        Task<bool> CheckIfFileExists(string folder, string languageId);
        Task<bool> CheckIfOldExists(string folder);

        Task<Dictionary<string, string>> GetLanguage(string folder, string languageCode);

        Task StoreLanguage(string folder, string LanguageCode, Dictionary<string, string> data);
    }
}
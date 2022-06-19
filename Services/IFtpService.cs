using JsonTranslator.Models;

namespace JsonTranslator.Services
{
    public interface IFtpService
    {
        Task<bool> CheckIfDefaultExists(FTP ftpt, string folder, string defaultLanguage);
        Task<bool> CheckIfFileExists(FTP ftpt, string folder, string languageId);
        Task<bool> CheckIfOldExists(FTP ftpt, string folder);
        Task<Dictionary<string, string>> GetLanguage(FTP ftpt, string folder, string languageCode);
        Task StoreLanguage(FTP ftpt, string folder, string LanguageCode, Dictionary<string, string> data);
    }
}
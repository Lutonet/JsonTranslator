using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Services
{
    public class FileService : IFileService
    {
        private ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger=logger;
        }

        public async Task<bool> CheckIfDefaultExists(string folder, string defaultLanguage)
        {
            string path = Path.Combine(folder, defaultLanguage + ".json");
            if (File.Exists(path))
                return await Task.FromResult(true);
            return await Task.FromResult(false);
        }

        public async Task<bool> CheckIfOldExists(string folder)
        {
            string path = Path.Combine(folder, "old.json");
            if (File.Exists(path))
                return await Task.FromResult(true);
            return await Task.FromResult(false);
        }

        public async Task<bool> CheckIfFileExists(string folder, string languageId)
        {
            string path = Path.Combine(folder, "languageId"+".json");
            if (File.Exists(path))
                return await Task.FromResult(true);
            return await Task.FromResult(false);
        }

        public async Task<Dictionary<string, string>> GetLanguage(string folder, string languageCode)
        {
            if (!await CheckIfFileExists(folder, languageCode))
            {
                return new Dictionary<string, string>();
            }

            string path = Path.Combine(folder, languageCode+".json");
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(path));
            if (result != null)
                return result;
            return new Dictionary<string, string>();
        }

        public async Task StoreLanguage(string folder, string LanguageCode, Dictionary<string, string> data)
        {
            string path = Path.Combine(folder, LanguageCode+".json");
            string json = JsonConvert.SerializeObject(data);
            try
            {
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Can't store file " + path + " Error: " + ex.Message);
            }
        }
    }
}
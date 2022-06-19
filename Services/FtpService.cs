using FluentFTP;
using JsonTranslator.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace JsonTranslator.Services
{
    public class FtpService : IFtpService
    {
        private ILogger<FtpService> _logger;

        public FtpService(ILogger<FtpService> logger)
        {
            _logger=logger;
        }

        public async Task<bool> CheckIfDefaultExists(FTP ftpt, string folder, string defaultLanguage)
        {
            string server = ftpt.Server;
            string path = Path.Combine(folder, defaultLanguage + ".json");
            using FtpClient client = new FtpClient(server, ftpt.Login, ftpt.Password);

            await client.AutoConnectAsync();

            if (await client.FileExistsAsync(path))
                return true;
            return false;
        }

        public async Task<bool> CheckIfOldExists(FTP ftpt, string folder)
        {
            string server = ftpt.Server;
            string path = Path.Combine(folder, "old.json");
            using FtpClient client = new FtpClient(server, ftpt.Login, ftpt.Password);

            await client.AutoConnectAsync();
            if (await client.FileExistsAsync(path))
                return true;
            return false;
        }

        public async Task<bool> CheckIfFileExists(FTP ftpt, string folder, string languageId)
        {
            string server = ftpt.Server;
            string path = Path.Combine(folder, "languageId"+".json");
            using FtpClient client = new FtpClient(server, ftpt.Login, ftpt.Password);

            await client.AutoConnectAsync();
            if (await client.FileExistsAsync(path))
                return true;
            return false;
        }

        public async Task<Dictionary<string, string>> GetLanguage(FTP ftpt, string folder, string languageCode)
        {
            string server = ftpt.Server;
            string temp = Path.GetTempPath();
            string name = languageCode+".tmp";
            string path = Path.Combine(temp, name);
            using FtpClient client = new FtpClient(server, ftpt.Login, ftpt.Password);

            if (!await CheckIfFileExists(ftpt, folder, languageCode))
            {
                return new Dictionary<string, string>();
            }

            string path2 = Path.Combine(folder, languageCode+".json");
            await client.ConnectAsync();
            await client.DownloadFileAsync(path, path2, FtpLocalExists.Overwrite, FtpVerify.Retry);

            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(await System.IO.File.ReadAllTextAsync(path));
            if (result != null)
                return result;
            return new Dictionary<string, string>();
        }

        public async Task StoreLanguage(FTP ftpt, string folder, string LanguageCode, Dictionary<string, string> data)
        {
            string server = ftpt.Server;
            string temp = Path.GetTempPath();
            string name = LanguageCode+".tmp";
            string path = Path.Combine(temp, name);
            string json = JsonConvert.SerializeObject(data);
            string path2 = Path.Combine(folder, LanguageCode+".json");
            using FtpClient client = new FtpClient(server, ftpt.Login, ftpt.Password);
            try
            {
                await System.IO.File.WriteAllTextAsync(path, json);
                await client.ConnectAsync();
                await client.UploadAsync(await System.IO.File.ReadAllBytesAsync(path), path2, FtpRemoteExists.Overwrite, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("Can't store file " + path + " Error: " + ex.Message);
            }
        }
    }
}
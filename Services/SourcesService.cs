using JsonTranslator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace JsonTranslator.Services
{
    public class SourcesService
    {
        private IConfiguration _configuration;
        private ILogger<SourcesService> _logger;
        private IFileService _file;
        private IFtpService _ftp;
        private ServiceSettings settings;
        private IApiService _api;
        private List<Language> allLanguages = new List<Language>();
        private List<Language> languagesToTranslate = new List<Language>();
        private string defaultLanguage;

        public SourcesService(IConfiguration configuration,
                              ILogger<SourcesService> logger,
                              IFileService file,
                              IFtpService ftp,
                              IApiService api)
        {
            _configuration=configuration;
            _logger=logger;
            _file=file;
            _ftp=ftp;
            _api=api;
            settings= _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
            defaultLanguage = settings.DefaultLanguage;
        }

        private async Task<List<Source>> LoadSources()
        {
            /* Check all FTPs and Folders and add them to the list of sources */
            string[] sourceFolders = settings.Folders;
            FTP[] fTPs = settings.FTPs;
            allLanguages = await _api.GetLanguages();
            List<Source> availableSources = new List<Source>();

            // Check Folders
            if (sourceFolders.Length != 0)
            {
                // check if source language exists in given folder
                foreach (string folder in sourceFolders)
                {
                    if (await _file.CheckIfDefaultExists(folder, defaultLanguage))
                        availableSources.Add(new Source() { File = folder, SourceType = Sources.Folder });
                }
            }
            _logger.LogInformation($"{availableSources.Count} folders added to the list");

            // Check FTPs
            if (settings.FTPs.Any())
                foreach (FTP ftp in settings.FTPs)
                {
                    for (int i = 0; i < ftp.Folder.Count; i++)
                        if (await _ftp.CheckIfDefaultExists(ftp, ftp.Folder[i], defaultLanguage))
                            availableSources.Add(new Source()
                            {
                                FtpSettings = ftp,
                                SourceType = Sources.Ftp,
                                FTPFolder = ftp.Folder[i]
                            }); ;
                }
            _logger.LogInformation($"Added {availableSources.Where(s => s.SourceType == Sources.Ftp).Count()} FTP folders");
            return availableSources;
        }

        private async Task<List<TranslationBulk>> GetNeededTranslations(Source source)
        {
            List<TranslationBulk> translationBulks = new List<TranslationBulk>();
            List<Language> neededTranslations = new();
            neededTranslations.AddRange(allLanguages);
            foreach (string language in settings.IgnoreLanguages)
            {
                neededTranslations.Remove(neededTranslations.Where(s => s.Code == language).FirstOrDefault());
            }
            neededTranslations.Add(new Language() { Code = "old" });
            foreach (Language language in neededTranslations)
            {
                TranslationBulk translationBulk = new TranslationBulk();
                translationBulk.Source = source;
                translationBulk.LanguageId = language.Code;
                if (source.SourceType == Sources.Folder)
                {
                    Dictionary<string, string> lang = await _file.GetLanguage(source.File, language.Code);
                    translationBulk.Dictionary = lang;
                }
                if (source.SourceType == Sources.Ftp)
                {
                    Dictionary<string, string> lang = await _ftp.GetLanguage(source.FtpSettings, source.FTPFolder, language.Code);
                    translationBulk.Dictionary = lang;
                }
                translationBulks.Add(translationBulk);
            }

            return translationBulks;
        }

        private async Task<Dictionary<string, string>> GetTranslation(Source source, string languageCode)
        {
            if (source.SourceType == Sources.Folder)
            {
                return await _file.GetLanguage(source.File, languageCode);
            }
            if (source.SourceType == Sources.Ftp)
            {
                return await _ftp.GetLanguage(source.FtpSettings, source.FTPFolder, languageCode);
            }
            else return new Dictionary<string, string>();
        }

        public async Task<TranslationWorkload> GetWorkload()
        {
            List<Source> sources = await LoadSources();
            foreach (Source source in sources)
            {
            }
            return new TranslationWorkload();
        }
    }
}
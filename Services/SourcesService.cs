using JsonTranslator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace JsonTranslator.Services
{
    public class SourcesService : ISourcesService
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
        private List<Source> availableSources = new List<Source>();

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
            Init().Wait();
        }

        public async Task Init()
        {
            // load storages
            availableSources.Clear();
            availableSources = await LoadSources();
        }

        private async Task<List<Source>> LoadSources()
        {
            /* Check all FTPs and Folders and add them to the list of sources */
            string[] sourceFolders = settings.Folders;
            FTP[] fTPs = settings.FTPs;
            allLanguages = await _api.GetLanguages();

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
            {
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
            }
            return availableSources;
        }

        private async Task<List<TranslationBulk>> GetNeededTranslations(Source source)
        {
            List<TranslationBulk> translationBulks = new List<TranslationBulk>();
            List<Language> neededTranslations = new();
            neededTranslations.AddRange(allLanguages);
            languagesToTranslate.Clear();
            foreach (string language in settings.IgnoreLanguages)
            {
                neededTranslations.Remove(neededTranslations.Where(s => s.Code == language).FirstOrDefault());
            }

            foreach (var line in neededTranslations)
            {
                languagesToTranslate.Add(allLanguages.Where(s => s.Code == line.Code).FirstOrDefault());
            }
            languagesToTranslate.Remove(languagesToTranslate.Where(s => s.Code == defaultLanguage).FirstOrDefault());

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
            List<Translation> toAdd = new List<Translation>();
            List<Translation> toRemove = new List<Translation>();
            foreach (Source source in availableSources)
            {
                Dictionary<string, string> toUpdate = new Dictionary<string, string>();
                List<TranslationBulk> translationsInSource = await GetNeededTranslations(source);
                Dictionary<string, string> defaultTranslation = translationsInSource
                    .Where(s => s.LanguageId == defaultLanguage)
                    .Select(s => s.Dictionary).FirstOrDefault();
                Dictionary<string, string> old = translationsInSource
                    .Where(s => s.LanguageId == "old")
                    .Select(s => s.Dictionary).FirstOrDefault();
                List<Language> lngs = new List<Language>();
                lngs.AddRange(languagesToTranslate);
                lngs.Remove(lngs.Where(s => s.Code == defaultLanguage).FirstOrDefault());
                // check changes between old and new file
                if (defaultTranslation != null && old != null && old.Count > 0)
                {
                    foreach (var line in defaultTranslation)
                    {
                        if (old.ContainsKey(line.Key))
                            if (old[line.Key] != defaultTranslation[line.Key])
                                toUpdate.Add(line.Key, line.Value);
                    }
                }

                // now check for changes in all other languages
                foreach (var language in lngs)
                {
                    TranslationBulk actualTranslation = translationsInSource.Where(s => s.LanguageId == language.Code).FirstOrDefault();
                    Dictionary<string, string> actualDictionary = actualTranslation.Dictionary;

                    // check what needs to be added
                    foreach (var line in defaultTranslation)
                    {
                        if (!actualDictionary.ContainsKey(line.Key))
                            toAdd.Add(new Translation()
                            {
                                Language = language.Code,
                                Phrase = line.Key,
                                Text = line.Value,
                                Source = source
                            });
                    }

                    // check what needs to be updated
                    if (toUpdate.Count > 0)
                    {
                        foreach (var update in toUpdate)
                        {
                            toAdd.Add(new Translation()
                            {
                                Language = language.Code,
                                Phrase= update.Key,
                                Text= update.Value,
                                Source= source
                            });
                        }
                    }

                    // check what needs to be removed
                    foreach (var line in actualDictionary)
                    {
                        if (!defaultTranslation.ContainsKey(line.Key))
                            toRemove.Add(new Translation()
                            {
                                Language = language.Code,
                                Phrase = line.Key,
                                Text = line.Value,
                                Source = source
                            });
                    }
                }
            }

            return new TranslationWorkload() { ToAdd = toAdd, ToRemove = toRemove };
        }

        public async Task<List<TranslationBulk>> GetAllNeededTranslations()
        {
            List<TranslationBulk> result = new();
            foreach (var source in availableSources)
            {
                var tmp = await GetNeededTranslations(source);
                result.AddRange(tmp);
            }
            return result;
        }

        public async Task StoreResults(List<TranslationBulk> toStore)
        {
            foreach (TranslationBulk toStoreItem in toStore)
            {
                if (toStoreItem.Source.SourceType == Sources.Folder)
                {
                    await (_file.StoreLanguage(toStoreItem.Source.File, toStoreItem.LanguageId, toStoreItem.Dictionary));
                }
                if (toStoreItem.Source.SourceType == Sources.Ftp)
                {
                    await (_ftp.StoreLanguage(toStoreItem.Source.FtpSettings, toStoreItem.Source.FTPFolder, toStoreItem.LanguageId, toStoreItem.Dictionary));
                }
            }
        }
    }
}
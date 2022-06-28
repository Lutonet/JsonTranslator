using JsonTranslator.Models;
using JsonTranslator.Services;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace JsonTranslator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApiService _api;
        private readonly IFileService _file;
        private readonly IFtpService _ftp;
        private readonly ISourcesService _sources;
        public ServiceSettings settings;
        private List<Language> languagesForTranslation = new List<Language>();
        private string defaultLanguage;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IApiService api, IFileService file, IFtpService ftp, ISourcesService sources)
        {
            _logger = logger;
            _configuration=configuration;
            _api=api;
            _file=file;
            _ftp=ftp;
            _sources=sources;
            settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
            defaultLanguage = settings.DefaultLanguage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int counter = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime processStart = DateTime.Now;

                while (!stoppingToken.IsCancellationRequested && settings == null || settings.DefaultLanguage == null)
                {
                    _logger.LogWarning("Settings not found, waiting 1s to retry");
                    settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
                    Task.Delay(500, stoppingToken).Wait();
                }
                _logger.LogInformation("Settings loaded");

                while (!await _api.TestServer() && !stoppingToken.IsCancellationRequested)
                {
                    Task.Delay(5000).Wait();
                    _logger.LogWarning("Waiting for APIs, No server available");
                }
                DateTime startLoadingFiles = DateTime.Now;
                // get original data
                _logger.LogInformation("Checking for the changes needed");
                List<TranslationBulk> allTranslations = await _sources.GetAllNeededTranslations();
                // get translators workload
                TranslationWorkload workload = await (_sources.GetWorkload());
                DateTime finishedExaminingChanges = DateTime.Now;
                int folders = workload.ToAdd.Where(s => s.Source.SourceType == Sources.Folder).ToList().Count;
                int ftpcount = workload.ToAdd.Where(s => s.Source.SourceType == Sources.Ftp).ToList().Count;
                _logger.LogInformation($"Source files loaded in {finishedExaminingChanges.Subtract(startLoadingFiles).TotalSeconds} s.");
                _logger.LogInformation($"Found {workload.ToAdd.Count} phrases to translate and {workload.ToRemove.Count} phrases to remove");

                // long lasting task
                DateTime translationsStart = DateTime.Now;
                List<TranslationBulk> result = await (_api.Translate(workload.ToAdd, stoppingToken));
                DateTime translationsStop = DateTime.Now;
                int totalseconds = (int)translationsStop.Subtract(translationsStart).TotalSeconds;
                int mins = totalseconds / 60;
                int seconds = totalseconds % 60;
                if (totalseconds == 0) totalseconds = 1;
                _logger.LogInformation($"Received {workload.ToAdd.Count} translations in {mins}:{seconds}");
                _logger.LogInformation($"Translated Speed is {workload.ToAdd.Count / totalseconds} phrases per second");

                // we have all translations, now we need to add them to original source files
                _logger.LogInformation("Storing changed and created translation files");
                DateTime startStoring = DateTime.Now;
                foreach (var translation in result)
                {
                    var actualItem = allTranslations.Where(s => s.Source == translation.Source && s.LanguageId == translation.LanguageId).FirstOrDefault();
                    if (translation.Dictionary != null)
                    {
                        foreach (var item in translation.Dictionary)
                            try
                            {
                                actualItem.Dictionary.Add(item.Key, item.Value);
                            }
                            catch
                            {
                                actualItem.Dictionary[item.Key] = item.Value;
                            }
                    }
                }
                foreach (var translation in allTranslations)
                {
                    var toRemove = workload.ToRemove.Where(t => t.Source == translation.Source && t.Language == translation.LanguageId).ToList();

                    foreach (var item in toRemove)
                    {
                        try
                        {
                            //remove
                            translation.Dictionary.Remove(item.Phrase);
                        }
                        catch
                        {
                            _logger.LogError("Phrase was not removed");
                            //do nothing
                        }
                    }
                }
                allTranslations
                    .Where(s => s.LanguageId == "old")
                    .FirstOrDefault().Dictionary
                    = allTranslations
                    .Where(s => s.LanguageId == defaultLanguage)
                    .Select(s => s.Dictionary)
                    .FirstOrDefault();

                await _sources.StoreResults(allTranslations);
                DateTime endStoring = DateTime.Now;
                _logger.LogInformation($"All files stored in {endStoring.Subtract(startStoring).Milliseconds} ms");
                _logger.LogInformation($"All work finished in {endStoring.Subtract(processStart).Minutes} min, {endStoring.Subtract(processStart).Seconds} sec");
                counter++;
                _logger.LogInformation($"Program run after restart: {counter}");
                _logger.LogInformation($"Program will run other check in 10 minutes");
                await Task.Delay(600000, stoppingToken);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
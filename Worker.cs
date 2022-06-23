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
        public ServiceSettings settings;
        private List<HttpClient> clients;
        private List<Language> allLanguages;
        private List<Language> languagesForTranslation;
        private List<String> folders;
        private List<FTP> ftps;
        private string defaultLanguage;
        private List<Task> backends;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IApiService api, IFileService file, IFtpService ftp)
        {
            _logger = logger;
            _configuration=configuration;
            _api=api;
            _file=file;
            _ftp=ftp;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                List<Translation> toAdd = new List<Translation>();
                List<Translation> toRemove = new List<Translation>();
                Dictionary<string, string> toUpdate = new Dictionary<string, string>();
                Dictionary<string, string> defaultDictionary = new Dictionary<string, string>();
                Dictionary<string, string> oldDefault = new Dictionary<string, string>();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                // Check folders
                while (folders == null && !stoppingToken.IsCancellationRequested)
                {
                    foreach (string folder in settings.Folders)
                    {
                        bool check = await _file.CheckIfDefaultExists(folder, defaultLanguage);
                        if (check)
                        {
                            if (folders == null) folders = new List<string>();
                            folders.Add(folder);
                        }
                    }
                    _logger.LogInformation($"Added {folders.Count} folders");
                    Task.Delay(200).Wait();
                }
                // Check FTP
                if (settings.FTPs.Any())
                    foreach (FTP ftp in settings.FTPs)
                    {
                        for (int i = 0; i < ftp.Folder.Count; i++)
                            if (await _ftp.CheckIfDefaultExists(ftp, ftp.Folder[i], defaultLanguage))
                                ftps.Add(ftp);
                    }
                _logger.LogInformation($"Added {ftps.Count} FTP folders");
                Task.Delay(200).Wait();
                // We have list of Folders - let check for changes one by one
                // directly add them to the to translate lists
                foreach (string folder in folders)
                {
                }
                // Go through folders

                await Task.Delay(10000, stoppingToken);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading program settings...");
            // load Settings
            while (!cancellationToken.IsCancellationRequested && settings == null || settings.DefaultLanguage == null)
            {
                _logger.LogWarning("Settings not found, waiting 1s to retry");
                settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
                Task.Delay(500, cancellationToken).Wait();
            }
            _logger.LogInformation("Settings loaded successfully");
            allLanguages = await _api.GetLanguages();
            foreach (Language language in allLanguages)
            {
                if (language.Code != defaultLanguage)
                {
                    if (!settings.IgnoreLanguages.Where(s => s == language.Code).Any())
                        languagesForTranslation.Add(language);
                }
            }
            _logger.LogInformation($"{languagesForTranslation.Count} to be checked");
            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
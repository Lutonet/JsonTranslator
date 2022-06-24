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
        private List<HttpClient> clients;
        private List<Language> allLanguages;
        private List<Language> languagesForTranslation = new List<Language>();
        private List<String> folders;
        private List<FTP> ftps;
        private string defaultLanguage;
        private List<Task> backends;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                TranslationWorkload workload = await (_sources.GetWorkload());
                //TODO we got workload now address api part
                _logger.LogInformation($"Found {workload.ToAdd.Count} phrases to add and {workload.ToRemove} phrases to remove");
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
using JsonTranslator.Models;
using JsonTranslator.Services;
using Microsoft.Extensions.Configuration;

namespace JsonTranslator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApiService _api;
        private readonly IFileService _file;
        public ServiceSettings settings;
        private List<HttpClient> clients;
        private List<Language> allLanguages;
        private List<Language> languagesForTranslation;
        private List<String> folders;
        private string defaultLanguage;
        private List<Task> backends;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IApiService api, IFileService file)
        {
            _logger = logger;
            _configuration=configuration;
            _api=api;
            _file=file;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Default language is: {l}", (defaultLanguage == null ? "not set" : defaultLanguage));
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
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
            Console.WriteLine("Settings Loaded");
            defaultLanguage = settings.DefaultLanguage;
            _logger.LogInformation($"Default language set to {defaultLanguage}");
            // Check folders
            while (folders == null && !cancellationToken.IsCancellationRequested)
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
                Task.Delay(500).Wait();
            }

            // Check FTP

            // Check API
            Servers[] servers = settings.Servers;
            foreach (Servers server in servers)
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(server.Address);
                if (clients == null) clients = new List<HttpClient>();
                clients.Add(client);
            }

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
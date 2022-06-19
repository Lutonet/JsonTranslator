using JsonTranslator.Models;

namespace JsonTranslator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private ServiceSettings settings;
        private List<Language> allLanguages;
        private List<Language> languagesForTranslation;
        private string defaultLanguage;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration=configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading program settings...");
            settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            defaultLanguage = settings.DefaultLanguage;
            _logger.LogInformation($"Default language set to {defaultLanguage}");
            // Check folders

            // Check FTP
            // Check API
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
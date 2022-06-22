using JsonTranslator.Models;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace JsonTranslator.Services
{
    public class ApiService : IApiService
    {
        public IConfiguration _configuration;
        public ServiceSettings settings;
        public static Servers[] servers;
        public static List<Translation> workload = new List<Translation>();
        public List<TranslationBulk> result = new List<TranslationBulk>();
        public static List<HttpClient> apiServers = new List<HttpClient>();
        private ILogger<ApiService> _logger;

        public ApiService(IConfiguration configuration, ILogger<ApiService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Init()
        {
            if (settings == null)
            {
                while (settings == null)
                {
                    _logger.LogWarning("Settings not found, waiting 1s to retry");
                    Task.Delay(1000).Wait();
                    settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
                }
                servers = settings.Servers;
            }
        }

        // most important class here
        public async Task<List<TranslationBulk>> Translate(List<Translation> phrases, CancellationToken token)
        {
            Init();
            // copy phrases to the local list
            foreach (var phrase in phrases)
            {
                workload.Add(phrase);
            }
            // create API instances and buffers

            // create worker and add it to the list
            return new List<TranslationBulk>();
        }

        // worker should include buffer and be able to call "fill buffer from shared list - elements moved to the buffer from the list
        public async Task<List<Language>> GetLanguages()
        {
            return await GetLanguages(apiServers[0]);
        }

        public async Task<List<Language>> GetLanguages(HttpClient client)
        {
            Init();
            return new List<Language>();
        }

        public async Task<ServerTestResult> TestServer(HttpClient client)
        {
            Init();
            return new ServerTestResult();
        }
    }
}
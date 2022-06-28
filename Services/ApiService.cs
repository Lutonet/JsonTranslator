using JsonTranslator.Models;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Net.Http.Json;

namespace JsonTranslator.Services
{
    public class ApiService : IApiService
    {
        public List<TranslationBulk> result = new List<TranslationBulk>();
        public IConfiguration _configuration;
        public ServiceSettings settings;
        public static Servers[] servers;
        private ILogger<ApiService> _logger;
        public List<HttpClient> apiServers = new List<HttpClient>();
        public List<BufferService> buffers = new List<BufferService>();
        public bool Completed = false;
        public List<Task> bufferTasks;

        public ApiService(IConfiguration configuration, ILogger<ApiService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            Init();
        }

        public void Init()
        {
            if (settings == null)
            {
                while (settings == null)
                {
                    _logger.LogWarning("Settings not found, waiting 1s to retry");
                    Task.Delay(200).Wait();
                    settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
                }
                servers = settings.Servers;
            }
        }

        // most important class here
        public async Task<List<TranslationBulk>> Translate(List<Translation> phrases, CancellationToken token)
        {
            List<Translation> workload = new List<Translation>();
            List<Translation> translated = new List<Translation>();
            List<Translation> errors = new List<Translation>();
            // copy phrases to the local list
            foreach (var phrase in phrases)
            {
                workload.Add(phrase);
            }
            // create API instances and buffers

            for (int i = 0; i < servers.Count(); i++)
            {
                buffers.Add(new BufferService(i, servers[i], 10, _configuration)); ;
            }
            int completedBuffers = 0;
            List<Task> bufferTask = new List<Task>();
            foreach (BufferService buffer in buffers.ToList())
            {
                bufferTask.Add(new Task(() => buffer.Start()));
            }
            foreach (Task buf in bufferTask)
            {
                buf.Start();
            }
            _logger.LogInformation($"{buffers.Count()} buffers started");

            while (!Completed && !token.IsCancellationRequested)
            {
                if (workload.Count > 0)
                {
                    foreach (BufferService service in buffers)
                    {
                        if (service.buffer.Count == 0)
                        {
                            if (workload.Count > 20)
                            {
                                if (!service.IsError)
                                {
                                    service.buffer.AddRange(workload.Take(service.Size));
                                    workload.RemoveRange(0, service.Size);
                                    _logger.LogInformation($"{workload.Count} phrases remain to be translated");
                                }
                            }
                            else if (workload.Count <= 20 && workload.Count > 0)
                            {
                                if (!service.IsError)
                                {
                                    service.buffer.Add(workload.FirstOrDefault());
                                    workload.Remove(workload.FirstOrDefault());
                                    _logger.LogInformation($"{workload.Count} phrases remain to be translated");
                                }
                            }

                            if (service.successfullTranslations.Any())
                            {
                                translated.AddRange(service.successfullTranslations);
                                service.successfullTranslations.Clear();
                            }
                            if (service.unsuccessfullTranslations.Any())
                            {
                                errors.AddRange(service.unsuccessfullTranslations);
                                service.unsuccessfullTranslations.Clear();
                            }
                        }
                    }
                }
                foreach (BufferService service in buffers)
                {
                    if (service.IsError)
                    {
                        _logger.LogError($"Buffer {service.id} is in Error state!");
                        if (service.buffer.Any())
                        {
                            workload.AddRange(service.buffer);
                            service.buffer.Clear();
                        }
                    }
                    if (service.buffer.Count == 0 && !service.successfullTranslations.Any() && !service.unsuccessfullTranslations.Any())
                    {
                        service.Finished = true;
                        completedBuffers++;
                    }
                    else
                    {
                        translated.AddRange(service.successfullTranslations);
                        errors.AddRange(service.unsuccessfullTranslations);
                        service.successfullTranslations.Clear();
                        service.unsuccessfullTranslations.Clear();
                    }
                }
                await Task.Delay(20);

                if (buffers.Where(s => s.Finished == true).Count() == buffers.Count())
                {
                    Completed = true;
                }

                // create worker and add it to the list
            }
            bufferTask.Clear();
            buffers.Clear();
            translated = translated.OrderBy(s => s.Language).ToList();
            string[] countries = translated.Select(s => s.Language).Distinct().ToArray();
            List<Source> sources = translated.Select(s => s.Source).Distinct().ToList();
            foreach (var source in sources)
            {
                List<List<Translation>> translationsInFolder = new List<List<Translation>>();
                foreach (var country in countries)
                {
                    translationsInFolder.Add(translated.Where(s => s.Source == source && s.Language == country).ToList());
                }
            }

            List<TranslationBulk> translations = new();

            if (countries != null && countries.Length > 0)
            {
                foreach (var source in sources)
                {
                    foreach (string country in countries)
                    {
                        List<Translation> localized = translated.Where(s => s.Language == country).Where(s => s.Source == source).ToList();
                        if (localized == null || localized.Count == 0)
                        {
                            translations.Add(new TranslationBulk() { LanguageId = country, Source=source, Dictionary = new Dictionary<string, string>() });
                        }
                        else
                        {
                            TranslationBulk bulk = new TranslationBulk();
                            bulk.LanguageId = country;
                            bulk.Source = source;
                            Dictionary<string, string> result = new Dictionary<string, string>();
                            foreach (Translation line in localized)
                            {
                                result.Add(line.Phrase, line.Text);
                            }
                            bulk.Dictionary = result;
                            translations.Add(bulk);
                        }
                    }
                }
                Completed = false;
                return translations;
            }
            Completed = false;
            return new List<TranslationBulk>();
        }

        // worker should include buffer and be able to call "fill buffer from shared list - elements moved to the buffer from the list
        public async Task<List<Language>> GetLanguages()
        {
            bool isError = true;
            int serversCount = servers.Length;
            int actual = 0;
            while (isError && actual < serversCount)
                try
                {
                    using HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(servers[actual].Address); ;
                    var response = await client.GetFromJsonAsync<List<Language>>("/languages");
                    if (response != null)
                    {
                        isError = false;
                        return response;
                    }
                }
                catch
                {
                    _logger.LogError($"Server {servers[actual].Address} is not available");
                    actual++;
                }
            return new List<Language>();
        }

        public async Task<bool> TestServer()
        {
            // this checks general availability of ANY server (for bare functionality)
            foreach (Servers server in servers)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(server.Address);
                    var response = await client.GetFromJsonAsync<List<Language>>("/languages");
                    if (response != null)
                        return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
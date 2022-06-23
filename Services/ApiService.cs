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
        public IConfiguration _configuration;
        public ServiceSettings settings;
        public static Servers[] servers;
        public static List<Translation> workload = new List<Translation>();
        public static List<Translation> translated = new List<Translation>();
        public static List<Translation> errors = new List<Translation>();
        public List<TranslationBulk> result = new List<TranslationBulk>();
        public static List<HttpClient> apiServers = new List<HttpClient>();
        private ILogger<ApiService> _logger;
        public List<BufferService> buffers = new List<BufferService>();
        public bool Completed = false;
        public List<Task> bufferTasks;

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
            for (int i = 0; i < servers.Count(); i++)
            {
                buffers.Add(new BufferService(servers[i], 5));
            }
            int completedBuffers = 0;
            int temp = 0;
            foreach (BufferService buffer in buffers)
            {
                Task bufferTask = new Task(async () => await buffer.Start());
            }
            while (!Completed && !token.IsCancellationRequested)
            {
                if (workload.Count > 0)
                {
                    foreach (BufferService service in buffers)
                    {
                        if (service.buffer.Count == 0)
                        {
                            if (workload.Count > service.Size)
                            {
                                service.buffer.AddRange(workload.Take(service.Size));
                                workload.RemoveRange(0, service.Size);
                            }
                            else
                            {
                                service.buffer.Add(workload.FirstOrDefault());
                                workload.Remove(workload.FirstOrDefault());
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
                    await Task.Delay(100);
                }
                foreach (BufferService service in buffers)
                {
                    if (service.buffer.Count == 0 && !service.successfullTranslations.Any() && !service.unsuccessfullTranslations.Any())
                    {
                        completedBuffers++;
                        service.Finished = true;
                        completedBuffers++;
                    }
                }
                Task.WaitAll(bufferTasks.ToArray());
                // create worker and add it to the list
                Console.WriteLine($"{translated.Count} phrases translated");
            }
            return new List<TranslationBulk>();
        }

        // worker should include buffer and be able to call "fill buffer from shared list - elements moved to the buffer from the list
        public async Task<List<Language>> GetLanguages()
        {
            Init();

            try
            {
                using HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(servers[0].Address);
                var response = await client.GetFromJsonAsync<List<Language>>("/languages");
                if (response != null)
                    return response;
            }
            catch
            {
                try
                {
                    using HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(servers[1].Address);
                    var response = await client.GetFromJsonAsync<List<Language>>("/languages");
                    if (response != null)
                        return response;
                }
                catch
                {
                    return new List<Language>();
                }
            }
            return new List<Language>();
        }

        public async Task<ServerTestResult> TestServer(HttpClient client)
        {
            Init();
            return new ServerTestResult();
        }
    }
}
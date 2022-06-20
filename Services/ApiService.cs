using JsonTranslator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Services
{
    public class ApiService : IApiService
    {
        public readonly IConfiguration _configuration;
        public readonly ServiceSettings settings;
        public static List<Servers> servers;
        public static List<Translation> workload = new List<Translation>();
        public List<TranslationBulk> result = new List<TranslationBulk>();
        public static List<HttpClient> apiServers = new List<HttpClient>();

        public ApiService(IConfiguration configuration)
        {
            settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
            servers = settings.Servers;
        }

        // most important class here
        public async Task<List<TranslationBulk>> Translate(List<Translation> phrases)
        {
            // copy phrases to the local list
            foreach (var phrase in phrases)
            {
                workload.Add(phrase);
            }
            // create API instances and buffers
            foreach (var server in servers)
            {
                // create worker and add it to the list
            }
            return new List<TranslationBulk>();
        }

        public class BufferService
        {
            private readonly HttpClient client;
            private readonly int size;
            private bool bufferError = true;

            public BufferService(int id, int size = 5)
            {
                client = apiServers[id];
                client.BaseAddress = new Uri(servers[id].Address);
                this.size = size;
            }

            public async Task Start()
            {
                List<Translation> buffer = new List<Translation>();

                // Initialize and start buffer work
                while (workload.Count > 0)
                {
                    // Test server before start and after each
                    if (bufferError)
                    {
                    }

                    if (buffer.Count == 0)
                    {
                        // get the next batch of phrases
                        try
                        {
                            buffer = workload.Take(size).ToList();
                        }
                        catch
                        {
                            try
                            {
                                buffer = workload.Take(1).ToList();
                            }
                            catch { };
                        }

                        workload.RemoveRange(0, buffer.Count);
                    }
                    // work through the buffer
                }
            }
        }

        // worker should include buffer and be able to call "fill buffer from shared list - elements moved to the buffer from the list
        public async Task<List<Language>> GetLanguages()
        {
            return await GetLanguages(apiServers[0]);
        }

        public async Task<(string, bool)> TranslatePhrase(string phrase, string targetLanguage, string sourceLanguage, HttpClient client)
        {
            List<Language> languages = await GetLanguages();
            Servers server = servers.Where(s => s.Address == client.BaseAddress.ToString()).FirstOrDefault();
            if (server == null)
                throw new Exception("server error");
            else
            {
                if (phrase == null
                    || targetLanguage == null
                    || sourceLanguage == null
                    || client== null
                    || phrase == string.Empty)
                    return ("Missing input", false);
                if (!languages.Any(s => s.Code == sourceLanguage))
                    return ("Source language doesn't exist", false);
                if (!languages.Any(s => s.Code == targetLanguage))
                    return ("Target language doesn't exist", false);

                // all seems OK let get translation
                return ("", true);
            }
        }

        public async Task<List<Language>> GetLanguages(HttpClient client)
        {
            return new List<Language>();
        }

        public async Task<ServerTestResult> TestServer(HttpClient client)
        {
            return new ServerTestResult();
        }
    }
}
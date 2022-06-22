using JsonTranslator.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Services
{
    public class BufferService
    {
        private IConfiguration _configuration;
        private Servers server;
        private ILogger<BufferService> _logger;
        private HttpClient client;
        public bool IsError { get; set; } = false;
        public bool Finished { get; set; } = false;

        public List<Translation> buffer = new List<Translation>();
        public List<Translation> successfullTranslations { get; set; }
        public List<Translation> unsuccessfullTranslations { get; set; }
        public List<Language> languagesToTranslate { get; set; }
        public string DefaultLanguage { get; set; }

        public BufferService(Servers server, int size = 5, IConfiguration configuration = null, ILogger<BufferService> logger = null)
        {
            _configuration = configuration;
            ServiceSettings settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
            DefaultLanguage = settings.DefaultLanguage;
            client = new HttpClient();
            client.BaseAddress = new Uri(server.Address);
            this.server = server;
            _logger = logger;
        }

        public async Task Start()
        {
            while (!Finished)
            {
                // Manage server in error - try each 1000ms
                while (IsError)
                {
                    if (!await IsServerAlive(server))
                    {
                        Task.Delay(1000).Wait();
                        _logger.LogWarning($"API server {server.Address} is not alive");
                    }
                    else
                    {
                        IsError = false;
                    }
                }

                //
                while (buffer.Count == 0)
                {
                    Task.Delay(50).Wait();
                }
            }
        }

        public async Task<bool> IsServerAlive(Servers server)
        {
            try
            {
                var response = await client.GetFromJsonAsync<List<Language>>("/languages");
                if (response == null) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Language>> GetLanguages(Servers server)
        {
            try
            {
                var response = await client.GetFromJsonAsync<List<Language>>("/languages");
                return response;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ApiResult> TranslatePhrase(TranslationRequestModel model)
        {
            var result = await client.PostAsJsonAsync("/translate", model);
            if (result.IsSuccessStatusCode)
            {
                try
                {
                    ApiTranslateResponse response = await result.Content.ReadFromJsonAsync<ApiTranslateResponse>();
                    return new ApiResult()
                    {
                        IsError = false,
                        Translation = response.translatedText
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Translation error on {server.Address} : {ex.Message}");
                    return new ApiResult()
                    {
                        IsError = true
                    };
                }
            }
            return new ApiResult()
            {
                IsError = true
            };
        }
    }
}
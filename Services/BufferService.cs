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
        public List<Translation> successfullTranslations { get; set; } = new List<Translation>();
        public List<Translation> unsuccessfullTranslations { get; set; } = new List<Translation>();
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

                // Cycle to check the buffer
                while (buffer.Count == 0)
                {
                    Task.Delay(20).Wait();
                }

                // buffer is not empty - let proceed
                foreach (Translation line in buffer)
                {
                    TranslationRequestModel request = new();
                    request.q = line.Text;
                    request.source = DefaultLanguage;
                    request.target = line.Language;
                    request.api_key = String.Empty;
                    if (server.UseKey)
                    {
                        request.api_key = server.Key;
                    }
                    ApiResult translationResult = await TranslatePhrase(request);
                    if (translationResult.IsError)
                    {
                        unsuccessfullTranslations.Add(line);
                        buffer.Remove(line);

                        //TODO translate all phrases
                    }
                    else
                    {
                        Translation successful = new Translation();
                        successful.Text = translationResult.Translation;
                        if (successful.Text == line.Text)
                        {
                            request.q = (request.q).ToLower();
                            translationResult = await TranslatePhrase(request);
                            if (!translationResult.IsError)
                            {
                                successfullTranslations.Add(new Translation()
                                {
                                    Phrase = line.Phrase,
                                    Text = translationResult.Translation,
                                    Language = line.Language
                                });
                            }
                        }
                        else
                        {
                            successfullTranslations.Add(new Translation()
                            {
                                Phrase = line.Phrase,
                                Text = translationResult.Translation,
                                Language = line.Language
                            });
                        }
                    }
                    // Buffer is empty let run another cycle
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
                    ApiTranslateResponse response = await result.Content.ReadFromJsonAsync<ApiTranslateResponse>();              // TODO - check if translation is correct => repeat in case of failure with uncapitalized letters
                    if (response != null)
                    {
                        return new ApiResult()
                        {
                            IsError = false,
                            Translation = response.translatedText
                        };
                    }
                    else
                    {
                        return new ApiResult()
                        {
                            IsError = true,
                            Translation = String.Empty
                        };
                    }
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
                IsError = true,
                Translation = result.StatusCode.ToString()
            };
        }
    }
}
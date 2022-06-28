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
        private ServiceSettings _settings;
        private Servers server;
        private ILogger<ApiService> _logger;
        private HttpClient client;
        public bool IsError { get; set; } = true;
        public bool Finished { get; set; } = false;
        public int Size { get; set; } = 5;
        public int id { get; set; }

        public List<Translation> buffer = new List<Translation>();
        public List<Translation> successfullTranslations { get; set; } = new List<Translation>();
        public List<Translation> unsuccessfullTranslations { get; set; } = new List<Translation>();
        public List<Language> languagesToTranslate { get; set; }
        public string DefaultLanguage { get; set; }

        public BufferService(int id, Servers server, int Size = 5, IConfiguration configuration = null, ILogger<ApiService> logger = null)
        {
            _configuration = configuration;
            _settings = _configuration.GetSection("ServiceSettings").Get<ServiceSettings>();
            DefaultLanguage = _settings.DefaultLanguage;
            client = new HttpClient();
            client.BaseAddress = new Uri(server.Address);
            this.server = server;
            _logger = logger;
            this.Size = Size;
            this.id = id;
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
                        Task.Delay(200).Wait();
                        _logger.LogWarning($"API server {server.Address} is not alive");
                        unsuccessfullTranslations.AddRange(buffer);
                        buffer.Clear();
                    }
                    else
                    {
                        IsError = false;
                    }
                }

                // Cycle to check the buffer
                while (buffer.Count == 0)
                {
                    if (Finished)
                        break;
                    Task.Delay(100).Wait();
                }
                if (buffer.Count > 0)
                {
                    // buffer is not empty - let proceed
                    foreach (Translation line in buffer)
                    {
                        if (IsError)
                        {
                            unsuccessfullTranslations.Add(line);
                            buffer.Remove(line);
                        }
                        if (line != null)
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
                                IsError = true;
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
                                            Language = line.Language,
                                            Source = line.Source
                                        });
                                    }
                                    else
                                    {
                                        unsuccessfullTranslations.Add(line);
                                        IsError = true;
                                    }
                                }
                                else
                                {
                                    successfullTranslations.Add(new Translation()
                                    {
                                        Phrase = line.Phrase,
                                        Text = translationResult.Translation,
                                        Language = line.Language,
                                        Source = line.Source
                                    });
                                }
                            }
                        }
                    }
                    // Buffer is empty let run another cycle
                }
                buffer.Clear();
            }
        }

        public async Task<bool> IsServerAlive(Servers server)
        {
            try
            {
                Task.Delay(50).Wait();
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
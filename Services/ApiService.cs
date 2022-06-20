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
        /**
         *
         * Get List of servers
         * Create Worker for each
         * Set Ready State
         *
         *
         *
         *
         */

        public ApiService()
        {
        }

        public async Task<List<TranslationBulk>> Translate(List<Translation> phrases, string sourceLanguage)
        {
            return new List<TranslationBulk>();
        }

        public async Task<List<Language>> GetLanguages()
        {
            return new List<Language>();
        }
    }
}
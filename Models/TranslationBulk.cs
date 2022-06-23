using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class TranslationBulk
    {
        public string LanguageId { get; set; }
        public Source Source { get; set; }
        public Dictionary<string, string> Dictionary { get; set; }
    }
}
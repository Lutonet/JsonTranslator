using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class TranslationRequestModel
    {
        public string q { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string format { get; set; } = "text";
        public string api_key { get; set; } = "";
    }
}
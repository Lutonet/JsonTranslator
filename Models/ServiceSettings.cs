using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class ServiceSettings
    {
        public Servers[] Servers { get; set; }
        public string [] Folders { get; set; }
        public FTP [] FTPs { get; set; }
        public string DefaultLanguage { get; set; }
        public string [] IgnoreLanguages { get; set; }
    }
}
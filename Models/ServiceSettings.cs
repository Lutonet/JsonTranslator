using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class ServiceSettings
    {
        public List<Servers> Servers { get; set; }
        public List<string> Folders { get; set; }
        public List<FTP> FTP { get; set; }
        public string DefaultLanguage { get; set; }
        public List<string> IgnoreLanguages { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class ServiceSettings
    {
        public List<Servers> Servers { get; }
        public List<string> Folders { get; }
        public List<FTP> FTP { get; }
        public string DefaultLanguage { get; }
        public List<string> IgnoreLanguages { get; }
    }
}
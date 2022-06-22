using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class FTP
    {
        public string Server { get; set; }
        public List<string> Folder { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
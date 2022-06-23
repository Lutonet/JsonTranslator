using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class Source
    {
        public Sources SourceType { get; set; }
        public FTP FtpSettings { get; set; }
        public string FTPFolder { get; set; }
        public string File { get; set; }
    }

    public enum Sources
    {
        Ftp, Folder
    };
}
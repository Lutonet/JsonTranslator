using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class Translation
    {
        public Source Source { get; set; }
        public string Phrase { get; set; }
        public string Text { get; set; }
        public string Language { get; set; }
    }
}
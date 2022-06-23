using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class TranslationWorkload
    {
        public List<Translation> ToRemove { get; set; }
        public List<Translation> ToAdd { get; set; }
    }
}
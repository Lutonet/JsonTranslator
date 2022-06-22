using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class Servers
    {
        public string Address { get; set; }
        public bool UseKey { get; set; } = false;
        public string Key { get; set; } = "";
    }
}
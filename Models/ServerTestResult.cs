using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class ServerTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ResponseTime { get; set; }
    }
}

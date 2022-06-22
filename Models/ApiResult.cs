using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonTranslator.Models
{
    public class ApiResult
    {
        public int Status { get; set; } = 200;
        public string Translation { get; set; } = "";
        public bool IsError { get; set; } = false;
    }
}
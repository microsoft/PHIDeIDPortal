using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace custom_skills.Models
{
    internal class PiiDetail
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public string Context { get; set; }
        public string Paragraph { get; set; }
    }
}

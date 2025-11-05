using System.Collections.Generic;

namespace custom_skills.Models
{
    internal class PiiDetectionResult
    {
        public bool PiiFound { get; set; }
        public List<PiiDetail> PiiDetails { get; set; } = [];
    }
}

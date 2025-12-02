using System.Collections.Generic;

namespace PhiDeidPortal.CustomFunctions.Entities
{
    internal class PiiDetectionResult
    {
        public bool PiiFound { get; set; }
        public List<PiiDetail> PiiDetails { get; set; } = [];
    }
}

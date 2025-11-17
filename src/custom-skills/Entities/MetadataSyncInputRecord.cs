namespace PhiDeidPortal.CustomFunctions.Entities
{
    public class MetadataSyncInputRecord
    {
        public class InputRecordData
        {
            public string Uri { get; set; }
            public string Status { get; set; }
        }

        public string RecordId { get; set; }
        public InputRecordData Data { get; set; }
    }
}
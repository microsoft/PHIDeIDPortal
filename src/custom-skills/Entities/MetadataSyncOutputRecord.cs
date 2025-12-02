namespace PhiDeidPortal.CustomFunctions.Entities
{
    public class MetadataSyncOutputRecord
    {
        public class OutputRecordData
        {
            public string Author { get; set; } = "";
            public int Status { get; set; } = 0;
            public string[] OrganizationalMetadata { get; set; } = [];
            public DateTime ExecutionTime { get { return DateTime.Now; } }
        }

        public class OutputRecordMessage
        {
            public string Message { get; set; }
        }

        public string RecordId { get; set; }
        public OutputRecordData Data { get; set; }
        public List<OutputRecordMessage> Errors { get; set; }
        public List<OutputRecordMessage> Warnings { get; set; }
    }
}
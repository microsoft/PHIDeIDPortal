namespace PhiDeidPortal.CustomFunctions.Entities
{
    public class WordRedactionInputRecord
    {
        public class InputRecordData
        {
            public string Text { get; set; }
            public string MaskingCharacter { get; set; }
            public string WordPattern { get; set; }
            public string Environment { get; set; } = "Default";
        }

        public string RecordId { get; set; }
        public InputRecordData Data { get; set; }
    }
}
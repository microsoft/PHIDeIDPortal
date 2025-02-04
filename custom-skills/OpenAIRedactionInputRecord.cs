namespace AISearch.CustomFunctions
{
    public class OpenAiRedactionInputRecord
    {
        public class InputRecordData
        {
            public string Text { get; set; }
            public string MaskingCharacter { get; set; }
        }

        public string RecordId { get; set; }
        public InputRecordData Data { get; set; }
    }
}
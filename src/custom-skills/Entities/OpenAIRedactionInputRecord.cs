namespace PhiDeidPortal.CustomFunctions.Entities
{
    public class OpenAiRedactionInputRecord
    {
        public class InputRecordData
        {
            public string Text { get; set; }
            public string MaskingCharacter { get; set; } = "*";
            public string MaxTokensPerParagraph { get; set; } = "4096";
            public string TokenOverlapSize { get; set; } = "25";
            public string Environment { get; set;  } = "Default";
        }

        public string RecordId { get; set; }
        public InputRecordData Data { get; set; }
    }
}
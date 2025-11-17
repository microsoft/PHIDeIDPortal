namespace PhiDeidPortal.CustomFunctions.Entities
{
        public class OpenAiRedactionOutputRecord
        {
            public class OutputRecordData
            {
                public string Text { get; set; } = "";
                public string RedactedText { get; set; } = "";
                public string RedactedEntities { get; set; } = "";
                public string MaxTokensPerParagraph { get; set; }
                public string TokenOverlapSize { get; set; }
                public string ParagraphCount { get; set; }

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
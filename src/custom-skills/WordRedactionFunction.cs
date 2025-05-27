using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace AISearch.CustomFunctions
{
    public class WordRedactionFunction
    {
        private readonly ILogger<WordRedactionFunction> _logger;

        public WordRedactionFunction(ILogger<WordRedactionFunction> logger)
        {
            _logger = logger;
        }

        [Function("WordRedactionFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var definition = new { Values = new List<WordRedactionInputRecord>()};
            var data = JsonConvert.DeserializeAnonymousType(requestBody, definition);

            if (data == null || data.Values == null)
            {
                return new BadRequestObjectResult("Please pass a valid request body");
            }

            var response = new
            {
                Values = new List<WordRedactionOutputRecord>()
            };

           // Calculate the response for each value.
            foreach (var record in data.Values)
            {
                if (record == null || record.RecordId == null) continue;

                var responseRecord = new WordRedactionOutputRecord
                {
                    RecordId = record.RecordId
                };

                try
                {
                    /* "PatternToMatch": "password,username,ssn,google"*/
                    var words = ((String)record.Data.WordPattern).Split(',');

                    var redactedWords = new List<String>();

                    responseRecord.Data = new WordRedactionOutputRecord.OutputRecordData()
                    {
                        
                        Text = record.Data.Text,
                        RedactedText = Regex.Replace(
                                        record.Data.Text, 
                                        string.Join("|", words.Select(item => $"(?:{item})")), 
                                        m => { redactedWords.Add(m.Value); return record.Data.MaskingCharacter; },
                                        RegexOptions.IgnoreCase),
                        RedactedEntities = string.Join(",", redactedWords)
                    };

                }
                catch (Exception e)
                {
                    var error = new WordRedactionOutputRecord.OutputRecordMessage
                    {
                        Message = e.Message
                    };

                    responseRecord.Errors = new List<WordRedactionOutputRecord.OutputRecordMessage>
                    {
                        error
                    };
                }
                finally
                {
                    response.Values.Add(responseRecord);
                }
            }

            return (ActionResult)new OkObjectResult(response); 
           
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Web;
using PhiDeidPortal.CustomFunctions.Entities;
using PhiDeidPortal.CustomFunctions.Services;

namespace PhiDeidPortal.CustomFunctions.Functions
{
    public class RegexRedactionFunction
    {
        private readonly ILogger<RegexRedactionFunction> _logger;

        public RegexRedactionFunction(ILogger<RegexRedactionFunction> logger)
        {
            _logger = logger;
        }

        [Function("RegexRedactionFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var definition = new { Values = new List<RegexRedactionInputRecord>()};
            var data = JsonConvert.DeserializeAnonymousType(requestBody, definition);

            if (data == null || data.Values == null)
            {
                return new BadRequestObjectResult("Please pass a valid request body");
            }

            var response = new
            {
                Values = new List<RegexRedactionOutputRecord>()
            };

           // Calculate the response for each value.
            foreach (var record in data.Values)
            {
                if (record == null || record.RecordId == null) continue;

                _logger.LogInformation($"Evaluating record {record.RecordId}");

                var featureService = new FeatureService();
                if (!featureService.IsFeatureEnabled(record.Data.Environment, Feature.RegexRedaction))
                {
                    response.Values.Add(new RegexRedactionOutputRecord
                    {
                        RecordId = record.RecordId,
                        Data = new RegexRedactionOutputRecord.OutputRecordData
                        {
                            Text = record.Data.Text,
                            RedactedText = record.Data.Text,
                            RedactedEntities = "[]"
                        },
                        Warnings = new List<RegexRedactionOutputRecord.OutputRecordMessage>
                        {
                            new() { Message = "Feature is not enabled." }
                        }
                    });

                    continue;
                }

                var responseRecord = new RegexRedactionOutputRecord
                {
                    RecordId = record.RecordId
                };

                try
                {
                    /* "PatternToMatch": "password,username,ssn,google"*/
                    var words = HttpUtility.UrlDecode(record.Data.RegexPattern).Split(',');

                    var redactedWords = new List<String>();

                    responseRecord.Data = new RegexRedactionOutputRecord.OutputRecordData()
                    {
                        
                        Text = record.Data.Text,
                        RedactedText = Regex.Replace(
                                        record.Data.Text, 
                                        string.Join("|", words.Select(item => $"(?:{item})")),
                                       //words[0],
                                        m => { redactedWords.Add(m.Value); return record.Data.MaskingCharacter; },
                                        RegexOptions.IgnoreCase),
                        RedactedEntities = string.Join(",", redactedWords)
                    };

                }
                catch (Exception e)
                {
                    var error = new RegexRedactionOutputRecord.OutputRecordMessage
                    {
                        Message = e.Message
                    };

                    responseRecord.Errors = new List<RegexRedactionOutputRecord.OutputRecordMessage>
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

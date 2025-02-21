using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using OpenAI.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AISearch.CustomFunctions;
using Microsoft.Extensions.Configuration;
using Azure.Core;
using Azure.Identity;
using System.Diagnostics;
using Azure;

namespace ChatCompletion;

public class OpenAI_StructuredOutputs
{
    private readonly ILogger<OpenAI_StructuredOutputs> log;

    public OpenAI_StructuredOutputs(ILogger<OpenAI_StructuredOutputs> logger)
    {
        log = logger;
    }

    [Function("OpenAiRedactionFunction")]
    public async Task<IActionResult> RedactSensitiveInfoWithOpenAI(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        // log.LogInformation("OpenAiRedactionFunction processed a request.");
        
        string redactionPrompt = Environment.GetEnvironmentVariable("PII_REDACTION_PROMPT") ?? "";
        // log.LogInformation($"Using redaction prompt: {redactionPrompt}");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        // log.LogInformation($"Received request body: {requestBody}");
        
        var definition = new { Values = new List<OpenAiRedactionInputRecord>()};
        var inputRecord = JsonConvert.DeserializeAnonymousType(requestBody, definition);
        
        if (inputRecord == null || inputRecord.Values == null)
        {
            log.LogError("Invalid request body received");
            return new BadRequestObjectResult("Please pass a valid request body");
        }

        log.LogInformation($"Processing {inputRecord.Values.Count} records");
        Kernel kernel = null;

        try
        {
            kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_NAME"),
                endpoint: Environment.GetEnvironmentVariable("OPENAI_ENDPOINT"),
                apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            )
            .Build();
        }
        catch (Exception ex)
        {
            log.LogError($"Error creating kernel: {ex}");

            if (ex.InnerException != null)
            {
                log.LogError($"Error creating kernel: {ex.InnerException}");
            }

            return new BadRequestResult();
        }

        log.LogInformation("Kernel created");

        ChatResponseFormat chatResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "piiDetectionResult",
            jsonSchema: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "PiiFound": {
                            "type": "boolean"
                        },
                        "PiiDetails": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "Text": { "type": "string" },
                                    "Type": { "type": "string" },
                                    "Context": { "type": "string" }
                                },
                                "required": ["Text", "Type", "Context"],
                                "additionalProperties": false
                            }
                        }
                    },
                    "required": ["PiiFound", "PiiDetails"],
                    "additionalProperties": false
                }
                """),
            jsonSchemaIsStrict: true);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            #pragma warning disable SKEXP0010
            ResponseFormat = chatResponseFormat
            #pragma warning restore SKEXP0010
        };

        PiiDetectionResult piiDetectionResult = new();
        try
        {
            var outputRecords = new
            {
                Values = new List<OpenAiRedactionOutputRecord>()
            };

        foreach (var record in inputRecord.Values)
        {
            if (record == null || record.RecordId == null) 
            {
                log.LogWarning("Skipping null record or record with null RecordId");
                continue;
            }

            log.LogInformation($"Processing record {record.RecordId}");
            var result = await kernel.InvokePromptAsync(redactionPrompt + record.Data.Text, new(executionSettings));
               
            var resultString = result.GetValue<string>();
            log.LogInformation($"OpenAI response for record {record.RecordId}: {resultString}");
            
            piiDetectionResult = JsonConvert.DeserializeObject<PiiDetectionResult>(resultString);
            log.LogInformation($"Found {piiDetectionResult.PiiDetails?.Count ?? 0} PII entities in record {record.RecordId}");

                var outputRecord = new OpenAiRedactionOutputRecord();
                outputRecord.RecordId = record.RecordId;
                log.LogInformation($"outputRecordID {outputRecord.RecordId}");
                outputRecord.Data = new OpenAiRedactionOutputRecord.OutputRecordData();
                outputRecord.Data.Text = record.Data.Text;
                log.LogInformation($"datatext {outputRecord.Data.Text}");
                log.LogInformation($"piiDetails {string.Join(", ", piiDetectionResult.PiiDetails.ConvertAll(o => $"Text: {o.Text} Content: {o.Context} Type: {o.Type}"))}");
                outputRecord.Data.RedactedText = piiDetectionResult.PiiFound
                        ? ApplyRedaction(record.Data.Text, piiDetectionResult.PiiDetails, record.Data.MaskingCharacter)
                        : record.Data.Text;
                log.LogInformation($"RedactedText {outputRecord.Data.RedactedText}");
                outputRecord.Data.RedactedEntities = JsonConvert.SerializeObject(piiDetectionResult.PiiDetails);
                log.LogInformation($"RedactedEntities {outputRecord.Data.RedactedEntities}");
                outputRecord.Errors = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>();
                log.LogInformation($"Errors {outputRecord.Errors}");
                outputRecord.Warnings = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>();
                log.LogInformation($"Warnings {outputRecord.Warnings}");

                outputRecords.Values.Add( outputRecord );
            }
        return new OkObjectResult(outputRecords);
        } catch (Exception ex)
        {
            log.LogInformation($"We goofed {ex}");
        }
        return new BadRequestResult();
    }

    private string ApplyRedaction(string text, List<PiiDetail> piiDetails, string maskingCharacter)
    {
        if (text == null || piiDetails == null || maskingCharacter == null) 
        {
            throw new Exception("ApplyRedaction requires a valid body");
        }

        string redactedText = text;

        foreach (var piiDetail in piiDetails)
        {
            log.LogInformation(piiDetail.Text);
            string maskedValue = new string(maskingCharacter.FirstOrDefault(), piiDetail.Text.Length);
            redactedText = redactedText.Replace(piiDetail.Text, maskedValue);
        }

        return redactedText;
    }
    
    #region private
    public struct PiiDetectionResult
    {
        public bool PiiFound { get; set; }
        public List<PiiDetail> PiiDetails { get; set; }
    }

    public struct PiiDetail
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public string Context { get; set; }
    }
    #endregion
}
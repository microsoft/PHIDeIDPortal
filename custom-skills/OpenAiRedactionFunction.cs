using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using OpenAI.Chat;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AISearch.CustomFunctions;
using Microsoft.Extensions.Configuration;
using Azure.Core;
using Azure.Identity;

namespace ChatCompletion;

public class OpenAI_StructuredOutputs()
{
    
    [Function("OpenAiRedactionFunction")]
    public async Task<IActionResult> RedactSensitiveInfoWithOpenAI(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        IConfiguration config = new ConfigurationBuilder()
           .AddEnvironmentVariables()
           .Build();
        string redactionPrompt = config["PII_REDACTION_PROMPT"] ?? "";
        
        bool useManagedIdentity = bool.Parse(Environment.GetEnvironmentVariable("USE_ENTRA_AUTH") ?? "false");
        TokenCredential credential = useManagedIdentity 
            ? new DefaultAzureCredential()
            : new AzureKeyCredential(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        OpenAiRedactionInputRecord inputRecord;

        try
        {
            inputRecord = JsonConvert.DeserializeObject<OpenAiRedactionInputRecord>(requestBody);
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult($"Invalid input format: {ex.Message}");
        }

        if (string.IsNullOrEmpty(inputRecord?.Data?.Text))
        {
            return new BadRequestObjectResult("The input 'Text' field is required.");
        }

        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME"),
                endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
                credential: credential)
            .Build();

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
            ResponseFormat = chatResponseFormat
        };

        var result = await kernel.InvokePromptAsync(redactionPrompt + inputRecord.Data.Text, new(executionSettings));

        var resultString = result.GetValue<String>();
        var piiDetectionResult = JsonConvert.DeserializeObject<PiiDetectionResult>(resultString);

        var outputRecord = new OpenAiRedactionOutputRecord
        {
            RecordId = inputRecord.RecordId,
            Data = new OpenAiRedactionOutputRecord.OutputRecordData
            {
                Text = inputRecord.Data.Text,
                RedactedText = piiDetectionResult.PiiFound
                    ? ApplyRedaction(inputRecord.Data.Text, piiDetectionResult.PiiDetails, inputRecord.Data.MaskingCharacter)
                    : inputRecord.Data.Text,
                RedactedEntities = JsonConvert.SerializeObject(piiDetectionResult.PiiDetails)
            },
            Errors = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>(),
            Warnings = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>()
        };

        return new OkObjectResult(piiDetectionResult);
    }

    private string ApplyRedaction(string text, List<PiiDetail> piiDetails, string maskingCharacter)
    {
        string redactedText = text;

        foreach (var piiDetail in piiDetails)
        {
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Text;
using Newtonsoft.Json;
using OpenAI.Chat;
using PhiDeidPortal.CustomFunctions.Entities;
using PhiDeidPortal.CustomFunctions.Services;

namespace PhiDeidPortal.CustomFunctions.Functions
{
    public class OpenAIRedactionFunction
    {
        private readonly ILogger<OpenAIRedactionFunction> _logger;

        public OpenAIRedactionFunction(ILogger<OpenAIRedactionFunction> logger)
        {
            _logger = logger;
        }

        [Function("OpenAiRedactionFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {

            string systemPrompt = Environment.GetEnvironmentVariable(EnvironmentVariables.PiiRedactionPrompt) ?? "";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var definition = new { Values = new List<OpenAiRedactionInputRecord>() };
            var inputRecord = JsonConvert.DeserializeAnonymousType(requestBody, definition);
        
            if (inputRecord == null || inputRecord.Values == null)
            {
                _logger.LogError("Invalid request body received");
                return new BadRequestObjectResult("Please pass a valid request body");
            }

            _logger.LogInformation($"Processing {inputRecord.Values.Count} records");
            Kernel kernel = null;

            try
            {
                _logger.LogInformation($"Deployment Name: {Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAiDeploymentName)}");
                _logger.LogInformation($"End Point: {Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAiEndpoint)}");
     
                kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAiDeploymentName),
                    endpoint: Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAiEndpoint),
                    apiKey: Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAiApiKey)
                )
                .Build();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating kernel: {ex}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Error creating kernel: {ex.InnerException}");
                }

                return new BadRequestResult();
            }

            _logger.LogInformation("Kernel created");

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

            PiiDetectionResult piiDetectionResult = new();
            try
            {
                var outputRecords = new
                {
                    Values = new List<OpenAiRedactionOutputRecord>()
                };

                var redact = kernel.CreateFunctionFromPrompt(
                    promptTemplate: "{{$text}}",
                    executionSettings: new OpenAIPromptExecutionSettings
                    {
                        ResponseFormat = chatResponseFormat,
                        Temperature = 0.0,
                        ChatSystemPrompt = systemPrompt
                    }
                );

                foreach (var record in inputRecord.Values)
                {
                    if (record == null || record.RecordId == null)
                    {
                        _logger.LogWarning("Skipping null record or record with null RecordId");
                        continue;
                    }

                    _logger.LogInformation($"Evaluating record {record.RecordId}");

                    var featureService = new FeatureService();
                    if (!featureService.IsFeatureEnabled(record.Data.Environment, Feature.OpenAiRedaction))
                    {
                        outputRecords.Values.Add(new OpenAiRedactionOutputRecord
                        {
                            RecordId = record.RecordId,
                            Data = new OpenAiRedactionOutputRecord.OutputRecordData
                            {
                                Text = record.Data.Text,
                                RedactedText = record.Data.Text,
                                MaxTokensPerParagraph = record.Data.MaxTokensPerParagraph,
                                TokenOverlapSize = record.Data.TokenOverlapSize,
                                ParagraphCount = "0",
                                RedactedEntities = "[]"
                            },
                            Warnings = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>
                            {
                                new() { Message = "Feature is not enabled." }
                            }
                        });

                        continue;
                    }

                    var prompt = systemPrompt + record.Data.Text;

                    _logger.LogInformation($"Input token count (with system prompt): {CountTokens(prompt)}");

                    var maxTokens = int.Parse(record.Data.MaxTokensPerParagraph);
                    var overlapSize = int.Parse(record.Data.TokenOverlapSize);
                    var paragraphs = SplitPlainTextParagraphs(record.Data.Text, maxTokens, overlapSize);

                    var invocations = await Task.WhenAll(paragraphs.Select(paragraph =>
                        kernel.InvokeAsync(redact, new KernelArguments { { "text", paragraph } })
                    ));

                    var invocationResults = new List<PiiDetectionResult>();
                    for (int i = 0; i < invocations.Length; i++)
                    {
                        var invocation = invocations[i];
                        var value = invocation.GetValue<string>();
                        if (string.IsNullOrWhiteSpace(value)) continue;

                        var result = JsonConvert.DeserializeObject<PiiDetectionResult>(value);
                        result.PiiDetails.ForEach(x => x.Paragraph = (i + 1).ToString());
                        invocationResults.Add(result);
                    }

                    piiDetectionResult = new PiiDetectionResult
                    {
                        // If any redaction has PiiFound=true, the merged result should have PiiFound=true
                        PiiFound = invocationResults.Any(r => r.PiiFound),
                        PiiDetails = invocationResults.SelectMany(r => r.PiiDetails ?? []).ToList()
                    };
  
                    var outputRecord = new OpenAiRedactionOutputRecord();
                    outputRecord.RecordId = record.RecordId;
                    outputRecord.Data = new OpenAiRedactionOutputRecord.OutputRecordData();
                    outputRecord.Data.MaxTokensPerParagraph = record.Data.MaxTokensPerParagraph;
                    outputRecord.Data.TokenOverlapSize = record.Data.TokenOverlapSize;
                    outputRecord.Data.ParagraphCount = paragraphs.Count.ToString();
                    outputRecord.Data.Text = record.Data.Text;
                    outputRecord.Data.RedactedText = piiDetectionResult.PiiFound
                            ? ApplyRedaction(record.Data.Text, piiDetectionResult.PiiDetails, record.Data.MaskingCharacter)
                            : record.Data.Text;
                    outputRecord.Data.RedactedEntities = JsonConvert.SerializeObject(piiDetectionResult.PiiDetails);
                    outputRecord.Errors = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>();
                    _logger.LogInformation($"Errors {outputRecord.Errors}");
                    outputRecord.Warnings = new List<OpenAiRedactionOutputRecord.OutputRecordMessage>();
                    _logger.LogInformation($"Warnings {outputRecord.Warnings}");

                    outputRecords.Values.Add(outputRecord);
                }

                return new OkObjectResult(outputRecords);

            } 
            catch (Exception ex)
            {
                _logger.LogInformation($"**** ERROR **** {Environment.NewLine}{ex}");
            }
            return new BadRequestResult();
        }

        #region private

        private string ApplyRedaction(string text, List<PiiDetail> piiDetails, string maskingCharacter)
        {
            if (text == null || piiDetails == null || maskingCharacter == null)
            {
                throw new Exception("ApplyRedaction requires a valid body");
            }

            string redactedText = text;

            foreach (var piiDetail in piiDetails)
            {
                string maskedValue = new string(maskingCharacter.FirstOrDefault(), piiDetail.Text.Length);
                redactedText = redactedText.Replace(piiDetail.Text, maskedValue);
            }

            return redactedText;
        }

        private int CountTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return 0; }

            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-3.5-turbo");
            var tokenCount = tokenizer.CountTokens(text);
            return tokenCount;
        }

        private List<string> SplitPlainTextParagraphs(string text, int maxTokensPerChunk, int tokenOverlapSize)
        {
            var counter = new TextChunker.TokenCounter(CountTokens);
            var chunker = TextChunker.SplitPlainTextParagraphs([text], maxTokensPerChunk, tokenOverlapSize, null, counter);
            return chunker;
        }

        #endregion

    }
}
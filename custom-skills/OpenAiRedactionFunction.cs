using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using OpenAI.Chat;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AISearch.CustomFunctions;


namespace ChatCompletion;

public class OpenAI_StructuredOutputs()
{
    
    [Function("OpenAiRedactionFunction")]
    public async Task<IActionResult> RedactSensitiveInfoWithOpenAI(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
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
                deploymentName: Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_NAME"),
                endpoint: Environment.GetEnvironmentVariable("OPENAI_ENDPOINT"),
                apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
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

        var result = await kernel.InvokePromptAsync("""
                Instructions:

                Identify PII: Include but are not limited to:

                Names, dates of birth, addresses, phone numbers, email addresses, social security numbers, medical record numbers.
                Employment details, educational history, specific location markers (e.g., home, work, or notable landmarks).
                Contextual mentions of specific affiliations (e.g., company names, organizational roles, job descriptions).
                Identify PHI: Include all health-related information that can be tied to an individual, such as:

                Medical diagnoses, treatment plans, medications, encounter dates, and vitals.
                Healthcare provider names, healthcare facility locations, or any other data that connects a person's health condition to their identity.
                Guidelines for Exclusions:

                Do not include text containing "[Redacted]" as PII or PHI since it has been anonymized.
                Generic or non-identifiable health information without associated PII should not be flagged as PHI.
                Output Details:

                Include a boolean property PiiFound to indicate whether any PII or PHI was found.
                For each detected item, provide:
                Text: The exact string that contains PII or PHI.
                Type: A classification (e.g., "Name," "Date of Birth," "Medical Record Number," "Diagnosis").
                Context: Additional surrounding text to provide clarity on the detected information.


                Example 1
                Example Input:

                "Employee ID: 123456789 is assigned to [Redacted]. Contact him at +1-555-123-4567 for urgent matters. The SSN is 987-65-4321."

                Example Output:
                {
                    "PiiFound": true,
                    "PiiDetails": [
                        {
                            "Text": "123456789",
                            "Type": "Employee ID",
                            "Context": "Employee ID: 123456789 is assigned to"
                        },
                        {
                            "Text": "+1-555-123-4567",
                            "Type": "Phone Number",
                            "Context": "Contact him at +1-555-123-4567 for urgent matters"
                        },
                        {
                            "Text": "987-65-4321",
                            "Type": "SSN",
                            "Context": "The SSN is 987-65-4321"
                        }
                    ]
                }
                Example 2
                Example Input:

                "The new hire, Jane Doe, uses jane.doe@example.com. Her office key card ID is 87654321. Please don't share this with [Redacted]."

                Example Output:
                {
                    "PiiFound": true,
                    "PiiDetails": [
                        {
                            "Text": "Jane Doe",
                            "Type": "Name",
                            "Context": "The new hire, Jane Doe, uses"
                        },
                        {
                            "Text": "jane.doe@example.com",
                            "Type": "Email",
                            "Context": "uses jane.doe@example.com"
                        },
                        {
                            "Text": "87654321",
                            "Type": "Office Key Card ID",
                            "Context": "Her office key card ID is 87654321"
                        }
                    ]
                }
                Example 3
                Example Input:

                "Please verify the transaction made by card ending in 4321. The account holder is [Redacted], but the transaction seems suspicious."

                Example Output:
                {
                    "PiiFound": false,
                    "PiiDetails": []
                }
                Example 4
                Example Input:

                "Here are the client details: Name: Alexander Hamilton, DOB: 1757-01-11, Address: 140 Hamilton Ave, Elizabethtown, NJ."

                Example Output:
                {
                    "PiiFound": true,
                    "PiiDetails": [
                        {
                            "Text": "Alexander Hamilton",
                            "Type": "Name",
                            "Context": "Name: Alexander Hamilton"
                        },
                        {
                            "Text": "1757-01-11",
                            "Type": "Date of Birth",
                            "Context": "DOB: 1757-01-11"
                        },
                        {
                            "Text": "140 Hamilton Ave, Elizabethtown, NJ",
                            "Type": "Address",
                            "Context": "Address: 140 Hamilton Ave, Elizabethtown, NJ"
                        }
                    ]
                }
                Example 5
                Example Input:

                "The document mentions John Smith, phone: +44 20 7946 0958, and passport number: 123456789. Make sure these details are secure."

                Example Output:
                {
                    "PiiFound": true,
                    "PiiDetails": [
                        {
                            "Text": "John Smith",
                            "Type": "Name",
                            "Context": "The document mentions John Smith"
                        },
                        {
                            "Text": "+44 20 7946 0958",
                            "Type": "Phone Number",
                            "Context": "phone: +44 20 7946 0958"
                        },
                        {
                            "Text": "123456789",
                            "Type": "Passport Number",
                            "Context": "passport number: 123456789"
                        }
                    ]
                }

                ACTUAL INPUT:

            """ + inputRecord.Data.Text, new(executionSettings));

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
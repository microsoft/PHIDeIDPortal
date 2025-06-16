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

        //string sampleRedactionPrompt;
        //sampleRedactionPrompt = """
        //       ATTENTION: Your task is to analyze input text sequentially through the following checks.
        //       If any prohibited data is identified, you MUST stop immediately and output the **Deny JSON**. 
        //       Only if ALL checks pass without finding prohibited data should you output the **Approve JSON**. 
        //       Output ONLY the JSON object.\\n \\n **Deny JSON Format (Use this EXACTLY when denying):**\\n 
        //       ```json\\n {\\n   \"PiiFound\": true,\\n   \"PiiDetails\": [ { \"text\": \"...\", \"type\": \"...\", \"context\": \"...\" } 
        //       /* List the FIRST finding(s) that triggered the denial here */ ]\\n }\\n ```\\n \\n 
        //       **Approve JSON Format (Use this EXACTLY if ALL checks pass):**\\n 
        //       ```json\\n {\\n   \"PiiFound\": false,\\n   \"PiiDetails\": []\\n }\\n ```\\n \\n 
        //       **Sequential Checks:**\\n \\n
        //       **Check 1: General PII/PHI Scan**\\n *   **Scan For:**\\n     *   Names (e.g., \"John Doe\", \"Smith, Jane\")\\n     *   Sub-State Geo Elements (e.g., \"123 Maple St\", \"Anytown\", \"Baltimore County\", \"90210\") - EXCLUDE state names like \"California\".\\n     *   Specific Dates (e.g., \"Born 01/15/1965\", \"Admitted: 10/08/2024\", \"Age: 92\")\\n     *   Contact Info (e.g., \"555-1234\", j.doe@email.com)\\n     *   IDs (e.g., SSN \"000-11-2222\", MRN \"MRN: 7654321\", Account \"Acct# 98765\", License \"DrvLic E5...\", VIN \"VIN: 1HG...\", Device ID \"Serial# ABC...\", IP \"192.168.1.100\", URL www.patientportal.com/johndoe)\\n     *   Biometrics/Images (Mention of fingerprints, voiceprints, full face photos)\\n     *   PHI Linkage: Any PII above found within 30 characters of a health condition/diagnosis/treatment (e.g., \"Emily Davis has IBS\", \"Asthma treatment for M. Johnson\")\\n *   **Action if Found:** STOP. Output the **Deny JSON**, listing the found item(s) in `PiiDetails` (e.g., `{\"text\": \"John Doe\", \"type\": \"Person\", \"context\": \"Patient Name: John Doe...\"}`).\\n *   **If Not Found:** Proceed to Check 2.\\n \\n 
        //       **Check 2: Specific Geocode Scan**\\n *   **Scan For:**\\n     *   Latitude/Longitude coordinates (e.g., \"Lat: 40.7128\", \"34.0522° N, 118.2437° W\")\\n     *   FIPS Codes (Pattern `\\b(\\d{2})\\s?(\\d{5})\\s?(\\d{6})\\b`, e.g., `\"24 24003 730101\"`, `\"1010005050808\"`)\\n *   **Action if Found:** STOP. Output the **Deny JSON**, listing the found geocode(s) in `PiiDetails` (e.g., `{\"text\": \"24 24003 730101\", \"type\": \"FIPS Code\", \"context\": \"... 24 24003 730101 1 ...\"}`).\\n *   **If Not Found:** Proceed to Check 3.\\n \\n 
        //       **Check 3: Ambiguity Scan**\\n *   **Scan For:** Data that is unclear or unfamiliar but *could potentially* represent one of the PII/PHI types from Check 1 or Check 2, or otherwise seems contextually sensitive under HIPAA, even if not a perfect match. Look for unusual codes, partial identifiers, or data points that lack clear non-sensitive context.\\n *   **Action if Found:** STOP. Output the **Deny JSON**, listing a generic ambiguity finding in `PiiDetails` (e.g., `{\"text\": \"[Ambiguous Data Found]\", \"type\": \"Ambiguity\", \"context\": \"Context surrounding the ambiguous data...\"}`).\\n *   **If Not Found:** Proceed to Check 4.\\n \\n 
        //       **Check 4: Approval**\\n *   **Condition:** This step is reached ONLY if Checks 1, 2, AND 3 passed without finding any prohibited data.\\n *   **Action:** Output the **Approve JSON**.\\n \\n Analyze the following text based ONLY on these sequential instructions:\\n";
        //       """;
        //
        // Now in Human readable format (translation courtesy of M365 Copilot)

        /*
         ATTENTION: Your task is to analyze input text sequentially through the following checks. If any prohibited data is identified, you MUST stop immediately and output the Deny JSON. Only if ALL checks pass without finding prohibited data should you output the Approve JSON. Output ONLY the JSON object.

        Deny JSON Format (Use this EXACTLY when denying): { "PiiFound": true, "PiiDetails": [ { "text": "...", "type": "...", "context": "..." } // List the FIRST finding(s) that triggered the denial here ] }

        Approve JSON Format (Use this EXACTLY if ALL checks pass): { "PiiFound": false, "PiiDetails": [] }

        Sequential Checks:

        Check 1: General PII/PHI Scan Scan For:

        Names (e.g., "John Doe", "Smith, Jane")
        Sub-State Geo Elements (e.g., "123 Maple St", "Anytown", "Baltimore County", "90210") - EXCLUDE state names like "California"
        Specific Dates (e.g., "Born 01/15/1965", "Admitted: 10/08/2024", "Age: 92")
        Contact Info (e.g., "555-1234", "j.doe@email.com")
        IDs (e.g., SSN "000-11-2222", MRN "MRN: 7654321", Account "Acct# 98765", License "DrvLic E5...", VIN "VIN: 1HG...", Device ID "Serial# ABC...", IP "192.168.1.100", URL "www.patientportal.com/johndoe")
        Biometrics/Images (Mention of fingerprints, voiceprints, full face photos)
        PHI Linkage: Any PII above found within 30 characters of a health condition/diagnosis/treatment (e.g., "Emily Davis has IBS", "Asthma treatment for M. Johnson") Action if Found: STOP. Output the Deny JSON, listing the found item(s) in PiiDetails. If Not Found: Proceed to Check 2.
        Check 2: Specific Geocode Scan Scan For:

        Latitude/Longitude coordinates (e.g., "Lat: 40.7128", "34.0522° N, 118.2437° W")
        FIPS Codes (Pattern: \b(\d{2})\s?(\d{5})\s?(\d{6})\b, e.g., "24 24003 730101", "1010005050808") Action if Found: STOP. Output the Deny JSON, listing the found geocode(s) in PiiDetails. If Not Found: Proceed to Check 3.
        Check 3: Ambiguity Scan Scan For: Data that is unclear or unfamiliar but could potentially represent one of the PII/PHI types from Check 1 or Check 2, or otherwise seems contextually sensitive under HIPAA. Look for unusual codes, partial identifiers, or data points that lack clear non-sensitive context. Action if Found: STOP. Output the Deny JSON, listing a generic ambiguity finding in PiiDetails. If Not Found: Proceed to Check 4.

        Check 4: Approval Condition: This step is reached ONLY if Checks 1, 2, AND 3 passed without finding any prohibited data. Action: Output the Approve JSON.
         */


        string redactionPrompt = Environment.GetEnvironmentVariable("PII_REDACTION_PROMPT") ?? "";
        log.LogInformation($"Using redaction prompt: {redactionPrompt}");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        
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
            log.LogInformation($"Deployment Name: {Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_NAME")}");
            log.LogInformation($"End Point: {Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")}");
            // log.LogInformation($"API Key: {Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Substring(0, 5)}*****{Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Substring(Environment.GetEnvironmentVariable("OPENAI_API_KEY").Length - 5)}"); // Removing this log as it may expose sensitive information

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
            log.LogInformation($"OpenAI response for record {record.RecordId}: {resultString}"); // ToDo: Check for potentially sensitive information in the response

                piiDetectionResult = JsonConvert.DeserializeObject<PiiDetectionResult>(resultString);
            log.LogInformation($"Found {piiDetectionResult.PiiDetails?.Count ?? 0} PII entities in record {record.RecordId}");

                var outputRecord = new OpenAiRedactionOutputRecord();
                outputRecord.RecordId = record.RecordId;
                log.LogInformation($"outputRecordID {outputRecord.RecordId}"); 
                outputRecord.Data = new OpenAiRedactionOutputRecord.OutputRecordData();
                outputRecord.Data.Text = record.Data.Text;
                log.LogInformation($"datatext {outputRecord.Data.Text}"); // ToDo: Check for potentially sensitive information in the response
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
            log.LogInformation($"**** ERROR **** {Environment.NewLine}{ex}");
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
            // log.LogInformation(piiDetail.Text); Removing this log as it may expose sensitive information
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
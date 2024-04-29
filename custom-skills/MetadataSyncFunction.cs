using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AISearch.CustomFunctions
{
    public class MetadataSyncFunction
    {
        private readonly ILogger<MetadataSyncFunction> _logger;
        private readonly CosmosDBService _cosmosDBService;

        public MetadataSyncFunction(ILogger<MetadataSyncFunction> logger)
        {
            _logger = logger;
            _cosmosDBService = new CosmosDBService(
                Environment.GetEnvironmentVariable("COSMOS_ENDPOINT_URI"),
                Environment.GetEnvironmentVariable("COSMOS_PRIMARY_KEY"),
                Environment.GetEnvironmentVariable("COSMOS_DATABASE_NAME"),
                Environment.GetEnvironmentVariable("COSMOS_CONTAINER_NAME"),
                Environment.GetEnvironmentVariable("COSMOS_PARTITION_KEY"));
        }

        [Function("MetadataSyncFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var definition = new { Values = new List<MetadataSyncInputRecord>()};
            var data = JsonConvert.DeserializeAnonymousType(requestBody,definition);

            if (data == null || data.Values == null)
            {
                return new BadRequestObjectResult("Please pass a valid request body");
            }

            var response = new
            {
                Values = new List<MetadataSyncOutputRecord>()
            };

            await _cosmosDBService.InitializeAsync();

           // Calculate the response for each value.
            foreach (var record in data.Values)
            {
                if (record == null || record.RecordId == null) continue;

                var responseRecord = new MetadataSyncOutputRecord
                {
                    RecordId = record.RecordId
                };

                try
                {

                    // Is there a record in the Cosmos DB that matches the data.Uri?
                    var cosmosRecord = await _cosmosDBService.GetItemByFieldAsync($"SELECT * FROM c WHERE c.Uri='{record.Data.Uri}'");

                    // If no, create a new record with input data. This should be created through the Web UI
                    if (cosmosRecord == null)
                    {
                        cosmosRecord = new CosmosRecord { 
                            id = Guid.NewGuid().ToString(),
                            Uri = record.Data.Uri, 
                            FileName = record.Data.Uri.Split('/').Last(),
                            Status = record.Data.Status,
                            JustificationText = String.Empty,
                            Author = "N/A",
                            LastIndexed = DateTime.UtcNow,
                            OrganizationalMetadata = []                          
                        };
                    }

                    var status = record.Data.Status;

                    // If yes, update the status in the Cosmos DB record if it is in status = 1 TODO: discuss
                    if (int.Parse(cosmosRecord.Status) == (int)DeidStatus.Uploaded && int.Parse(record.Data.Status) > (int)DeidStatus.Uploaded)
                    {
                        cosmosRecord.Status = status;
                    }
                    else 
                    {
                        status = cosmosRecord.Status;
                    }

                    cosmosRecord.LastIndexed = DateTime.UtcNow;

                    // Update status on the Cosmos DB record
                    await _cosmosDBService.UpsertItemAsync(cosmosRecord);

                    responseRecord.Data = new MetadataSyncOutputRecord.OutputRecordData()
                    {
                            Author = cosmosRecord.Author,
                            Status = int.Parse(status),
                            OrganizationalMetadata = cosmosRecord.OrganizationalMetadata
                    };

                }
                catch (Exception e)
                {
                    var error = new MetadataSyncOutputRecord.OutputRecordMessage
                    {
                        Message = e.Message
                    };

                    responseRecord.Errors = new List<MetadataSyncOutputRecord.OutputRecordMessage>
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


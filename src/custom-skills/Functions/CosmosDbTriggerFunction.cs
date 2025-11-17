using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PhiDeidPortal.CustomFunctions.Entities;
using PhiDeidPortal.CustomFunctions.Services;

namespace PhiDeidPortal.CustomFunctions.Functions
{
    public class Function
    {
        private readonly ILogger _logger;
        private readonly string _databaseName;
        private readonly string _containerName;
        private readonly HubConnection _hubConnection;
        private static readonly HttpClient client = new HttpClient();
        private readonly string _signalRHubUrl;

        public Function(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Function>();
            _databaseName = configuration["databaseName"];
            _containerName = configuration["containerName"];
            _signalRHubUrl = configuration["SignalRHubUrl"];

            _hubConnection = new HubConnectionBuilder()
                                .WithUrl(_signalRHubUrl)
                                .AddJsonProtocol(options =>
                                {
                                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                    options.PayloadSerializerOptions.WriteIndented = true;
                                })
                                .Build();
        }

        [Function("CosmosDbTriggerFunction")]
        public async Task Run([CosmosDBTrigger(
                                databaseName: "%databaseName%",
                                containerName: "%containerName%",
                                Connection = "dbConnection",
                                LeaseContainerName = "leases",
                                CreateLeaseContainerIfNotExists = true)] IReadOnlyList<object> objects)
        {
            if (objects != null && objects.Count > 0)
            {
                try
                {
                    foreach (var obj in objects)
                    {
                        CosmosRecord cosmosRecord = null;

                        if (obj is CosmosRecord record)
                        {
                            cosmosRecord = record;
                        }
                        else if (obj is JsonElement jsonElement)
                        {
                            try
                            {
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                                };
                                options.Converters.Add(new StatusConverterService());

                                var cosmosRecordRaw = JsonSerializer.Deserialize<CosmosRecordRaw>(jsonElement.GetRawText(), options);

                                if (cosmosRecordRaw != null)
                                {
                                    cosmosRecord = new CosmosRecord
                                    {
                                        id = cosmosRecordRaw.id,
                                        Uri = cosmosRecordRaw.Uri,
                                        FileName = cosmosRecordRaw.FileName,
                                        Status = cosmosRecordRaw.Status.ToString(),
                                        Author = cosmosRecordRaw.Author,
                                        AwaitingIndex = cosmosRecordRaw.AwaitingIndex,
                                        JustificationText = cosmosRecordRaw.JustificationText,
                                        LastIndexed = cosmosRecordRaw.LastIndexed,
                                        OrganizationalMetadata = cosmosRecordRaw.OrganizationalMetadata
                                    };
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogError(jsonEx, $"Error deserializing JSON to CosmosRecordRaw: {jsonElement.GetRawText()}");
                                continue; // Skip this object and continue with the next one
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Received an object that is unknown. {obj.GetType()}");
                            continue; // Skip this object and continue with the next one
                        }

                        if (cosmosRecord != null)
                        {
                            _logger.LogInformation($"Processing CosmosRecord with ID: {cosmosRecord.id}");
                            await SendHubMessage(cosmosRecord);
                        }
                    }
                }
                catch (HubException hex)
                {
                    _logger.LogError(hex, "Error sending data to SignalR hub");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Cosmos DB trigger");
                }
                finally
                {
                    await _hubConnection.StopAsync();
                }
            }
        }


        public async Task SendHubMessage(CosmosRecord cosmosRecord)
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                }
                catch (Exception startEx)
                {
                    _logger.LogError(startEx, "Error starting SignalR hub connection");
                    return; // Exit the method if the connection could not be started
                }
            }

            await _hubConnection.SendAsync("UpdateCounts", cosmosRecord);
        }
    }
}
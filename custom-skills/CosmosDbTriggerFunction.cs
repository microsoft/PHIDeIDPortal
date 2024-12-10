using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CosmosDBMonitor
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
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<object> documents)
        {
            if (documents != null && documents.Count > 0)
            {
                //_logger.LogInformation("Documents modified: " + input.Count);
                //_logger.LogInformation("First document Id: " + input[0].id);
                var message = new { user = "tommy", message = "hi, friend." };

                try
                {
                    foreach (var doc in documents)
                    {
                        // await client.PostAsJsonAsync(signalRHubUrl, message);


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

                        await _hubConnection.SendAsync("UpdateCounts");
                    }
                }
                catch (HubException hex)
                {
                    _logger.LogError(hex, "Error sending data to SignalR hub");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending data to SignalR hub");
                }
                finally
                {
                    await _hubConnection.StopAsync();
                }
            }
        }
    }

    public class Metadata
    {
        public string id { get; set; }
        public string Uri { get; set; }
        public string FileName { get; set; }
        public int Status { get; set; }
        public string Author { get; set; }
        public string[] OrganizationalMetadata { get; set; }
        public string JustificationText { get; set; }
        public bool AwaitingIndex { get; set; }
        public DateTime LastIndexed { get; set; }
    }
}
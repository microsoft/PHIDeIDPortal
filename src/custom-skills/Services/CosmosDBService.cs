using Microsoft.Azure.Cosmos;
using PhiDeidPortal.CustomFunctions.Entities;
using static PhiDeidPortal.CustomFunctions.Entities.EnvironmentVariables;

namespace PhiDeidPortal.CustomFunctions.Services
{
    public class CosmosDBService
    {
        private readonly string _endpointUri;
        private readonly string _primaryKey;
        private readonly string _databaseName;
        private readonly string _containerName;
        private readonly string _partitionKey;

        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public CosmosDBService()
        {
            _endpointUri = Environment.GetEnvironmentVariable(CosmosEndpointUri);
            _primaryKey = Environment.GetEnvironmentVariable(CosmosPrimaryKey);
            _databaseName = Environment.GetEnvironmentVariable(CosmosDatabaseName);
            _containerName = Environment.GetEnvironmentVariable(CosmosContainerName);
            _partitionKey = Environment.GetEnvironmentVariable(CosmosPartitionKey);
        }

        public async Task InitializeAsync()
        {
            _cosmosClient = new CosmosClient(_endpointUri, _primaryKey);
            _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            _container = await _database.CreateContainerIfNotExistsAsync(_containerName, _partitionKey);
        }

        public async Task UpsertItemAsync(dynamic item)
        {
            string itemId = item.id;
            await _container.UpsertItemAsync(item); 
            Console.WriteLine($"Upserted item with id: {itemId}");
        }

        public async Task<CosmosRecord> GetItemByFieldAsync(string query)
        {
            var limitQuery = $"{query} OFFSET 0 LIMIT 1";
            var queryDefinition = new QueryDefinition(limitQuery);
            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosRecord>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                var response = await queryResultSetIterator.ReadNextAsync();

                return response.FirstOrDefault();
            }

            return null;
        }
           
    }
}
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.Entities;
using System;
using System.Net;

namespace PhiDeidPortal.Ui.Services
{
    public class CosmosService : ICosmosService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _cosmosConfiguration;
        private readonly string _cosmosDbName;
        private readonly string _cosmosContainerName;
        private readonly string _cosmosPartitionKey;

        public CosmosService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            _configuration = configuration;

            _cosmosConfiguration = configuration.GetSection("CosmosDb");
            _cosmosDbName = _cosmosConfiguration["DatabaseId"] ?? throw new ArgumentNullException(nameof(_cosmosDbName));
            _cosmosContainerName = _cosmosConfiguration["ContainerId"] ?? throw new ArgumentNullException(nameof(_cosmosContainerName));
            _cosmosPartitionKey = _cosmosConfiguration["PartitionKey"] ?? throw new ArgumentNullException(nameof(_cosmosPartitionKey));
        }

        public List<MetadataRecord> GetAllMetadataRecords()
        {
            return GetMetadataRecords().ToList();
        }

        public List<MetadataRecord> GetAllMetadataRecordsByAuthor(string author)
        {
            return GetMetadataRecords().Where(d => (d.Author == author || d.Author == "N/A")).ToList();
        }

        public MetadataRecord? GetMetadataRecordById(string docId)
        {
            return GetMetadataRecords().Where(d => d.id == docId).FirstOrDefault();
        }

        public MetadataRecord? GetMetadataRecordByUri(string uri)
        {
            return GetMetadataRecords().Where(d => d.Uri == uri).FirstOrDefault();
        }

        public MetadataRecord? GetMetadataRecordByUriAndAuthor(string uri, string author)
        {
            return GetMetadataRecords().Where(d => d.Uri == uri && (d.Author == author || d.Author == "N/A")).FirstOrDefault();
        }

        public List<MetadataRecord> GetMetadataRecordsByStatus(int status)
        {
            return GetMetadataRecords().Where(d => (d.Status == status)).ToList();
        }

        public List<MetadataRecord> GetMetadataRecordsByStatusAndAuthor(int status, string author)
        {
            return GetMetadataRecords().Where(d => d.Status == status && (d.Author == author || d.Author == "N/A")).ToList();
        }

        private IOrderedQueryable<MetadataRecord> GetMetadataRecords()
        {
            return _cosmosClient
            .GetDatabase(_cosmosDbName)
            .GetContainer(_cosmosContainerName)
            .GetItemLinqQueryable<MetadataRecord>(true);
        }

        public StatusSummary GetSummaryByAuthor(string username)
        {
            return GetSummaryAsync($"SELECT * FROM c where c.Author = '{username}'").Result;
        }

        public StatusSummary GetSummary()
        {
            return GetSummaryAsync("SELECT * FROM c").Result;
        }

        private async Task<StatusSummary> GetSummaryAsync(string query)
        {
            var cosmosResult = _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .GetItemQueryIterator<dynamic>(query);

            var summaryResponse = new List<dynamic>();
            while (cosmosResult.HasMoreResults)
            {
                FeedResponse<dynamic> response = await cosmosResult.ReadNextAsync();
                summaryResponse.AddRange(response);
            }

            return new StatusSummary()
            {
                TotalCount = summaryResponse.Count(),
                UnprocessedCount = summaryResponse.Count(x => x.Status == 1),
                JustificationCount = summaryResponse.Count(x => x.Status == 2),
                ReviewCount = summaryResponse.Count(x => x.Status == 3),
                ApprovedCount = summaryResponse.Count(x => x.Status == 4),
                DeniedCount = summaryResponse.Count(x => x.Status == 5)
            };
        }

        public async Task<ItemResponse<MetadataRecord>> UpsertMetadataRecordAsync(MetadataRecord record)
        {
            var cosmosDbResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosDbName);
            var containerProperties = new ContainerProperties
            {
                PartitionKeyPath = _cosmosPartitionKey,
                Id = _cosmosContainerName
            };

            var containerResponse = await cosmosDbResponse.Database.CreateContainerIfNotExistsAsync(containerProperties);

            ItemResponse<MetadataRecord> recordResponse = await containerResponse.Container.UpsertItemAsync<MetadataRecord>(
            item: record,
                new PartitionKey(record.Uri)
                );

            return recordResponse;
        }

        public async Task<ServiceResponse> DeleteMetadataRecordAsync(MetadataRecord document)
        {
            var response = await _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .DeleteItemAsync<MetadataRecord>(document.id, new PartitionKey(document.Uri));

            return new ServiceResponse() { IsSuccess = response.StatusCode == HttpStatusCode.NoContent, Code = response.StatusCode };
        }
    }
}

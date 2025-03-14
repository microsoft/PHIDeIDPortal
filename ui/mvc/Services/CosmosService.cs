using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
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

        public async Task<List<MetadataRecord>> QueryMetadataRecords(List<CosmosFieldQueryValue> fieldValues)
        {
            var baseQuery = $"SELECT * FROM c";
            var optionalValues = new List<string>();
            var requiredValues = new List<string>();
            var queryParams = new List<SqlParameter>();

            if (fieldValues.Count > 0)
            {
                int paramIndex = 0;
                fieldValues.ForEach(x =>
                {
                    string paramName = $"@param{paramIndex++}";
                    string queryCondition = GetQueryCondition(x, paramName);

                    if (x.IsPrefixMatch) { queryParams.Add(new SqlParameter(paramName, x.FieldValue.ToString() + "%")); }
                    else { queryParams.Add(new SqlParameter(paramName, x.FieldValue.ToString())); }

                    if (x.IsRequired) { requiredValues.Add(queryCondition); }
                    else { optionalValues.Add(queryCondition); }
                });

                var optionalQueryString = string.Join(" OR ", optionalValues);
                var requiredQueryString = string.Join(" AND ", requiredValues);

                if (!string.IsNullOrEmpty(optionalQueryString))
                {
                    optionalQueryString = $"({optionalQueryString})";
                    optionalQueryString += string.IsNullOrEmpty(requiredQueryString) ? string.Empty : " AND ";
                }

                baseQuery += $" WHERE {optionalQueryString}{requiredQueryString}";
            }

            var def = new QueryDefinition(baseQuery);
            queryParams.ForEach(p => def.WithParameter(p.ParameterName, p.Value));

            var cosmosResult = _cosmosClient
            .GetDatabase(_cosmosDbName)
            .GetContainer(_cosmosContainerName)
            .GetItemQueryIterator<MetadataRecord>(def);

            var response = new List<MetadataRecord>();
            while (cosmosResult.HasMoreResults)
            {
                FeedResponse<MetadataRecord> feedResponse = await cosmosResult.ReadNextAsync();
                response.AddRange(feedResponse);
            }

            return response;

        }

        private static string GetQueryCondition(CosmosFieldQueryValue fieldValue, string paramName)
        {
            if (fieldValue.FieldValue is string)
            {
                return fieldValue.IsPrefixMatch
                    ? $"c.{fieldValue.FieldName} LIKE {paramName}"
                    : $"c.{fieldValue.FieldName} = {paramName}";
            }
            else if (fieldValue.FieldValue is int)
            {
                return $"ToString(c.{fieldValue.FieldName}) = {paramName}";
            }
            else
            {
                throw new InvalidOperationException("Unsupported field value type");
            }
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

            var response = new List<dynamic>();
            while (cosmosResult.HasMoreResults)
            {
                FeedResponse<dynamic> feedResponse = await cosmosResult.ReadNextAsync();
                response.AddRange(feedResponse);
            }

            return new StatusSummary()
            {
                TotalCount = response.Count(),
                UnprocessedCount = response.Count(x => x.Status == 1),
                JustificationCount = response.Count(x => x.Status == 2),
                ReviewCount = response.Count(x => x.Status == 3),
                ApprovedCount = response.Count(x => x.Status == 4),
                DeniedCount = response.Count(x => x.Status == 5)
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

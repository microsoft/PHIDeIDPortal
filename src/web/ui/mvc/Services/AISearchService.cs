using Azure.Search.Documents.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using static System.Net.WebRequestMethods;
using System.Net;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Azure.Cosmos.Linq;

namespace PhiDeidPortal.Ui.Services
{
    // Can we rename this class to the IndexService or AIIndexService, something like that?
    public class AISearchService : IAISearchService
    {
        private readonly IConfigurationSection _searchServiceConfiguration;
        private readonly string _searchUri;
        private readonly string _searchApiKey;
        private readonly string _indexName;
        private readonly string _defaultIndexerName;
        private readonly Uri _serviceEndpoint;
        private readonly AzureKeyCredential _credential;
        private readonly SearchClient _searchClient;
        private readonly HttpClient _httpClient;
        private readonly SearchIndexerClient _indexerClient;

        public AISearchService(IConfiguration configuration)
        {
            _searchServiceConfiguration = configuration.GetSection("SearchService");
            _searchUri = _searchServiceConfiguration["Uri"] ??= "";
            _searchApiKey = _searchServiceConfiguration["ApiKey"] ??= "";
            _indexName = _searchServiceConfiguration["IndexName"] ??= "";
            _defaultIndexerName = _searchServiceConfiguration["DefaultIndexerName"] ??= "";

            _serviceEndpoint = new Uri(_searchUri);
            _credential = new AzureKeyCredential(_searchApiKey);
            _searchClient = new SearchClient(_serviceEndpoint, _indexName, _credential);
            _indexerClient = new SearchIndexerClient(_serviceEndpoint, _credential);
            _httpClient = new HttpClient();
        }

        public async Task<Pageable<SearchResult<SearchDocument>>> SearchAsync(string? filter, string? searchString = "*")
        {
            var options = (filter is null) ? new SearchOptions() { SearchMode = SearchMode.All } : new SearchOptions() { Filter = filter, SearchMode = SearchMode.All };
            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(searchString, options);
            return response.GetResults();
        }

        public async Task<Pageable<SearchResult<SearchDocument>>> SearchByAuthorAsync(string author, string? filter, string? searchString = "*")
        {
            var options = (filter is null) ? new SearchOptions() { SearchMode = SearchMode.All } : new SearchOptions() { Filter = filter, SearchMode = SearchMode.All };
            searchString += $"+{author}";
            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(searchString, options);
            return response.GetResults();
        }

        public async Task<ServiceResponse> ResetDocumentAsync(string key)
        {
            // Reset document is in preview.
            var uri = $"{_searchUri}/indexers/{_defaultIndexerName}/resetdocs?api-version=2020-06-30-Preview";
            _httpClient.DefaultRequestHeaders.Remove("api-key");
            _httpClient.DefaultRequestHeaders.Add("api-key", _searchApiKey);
            _httpClient.DefaultRequestHeaders.Remove("contentType");
            _httpClient.DefaultRequestHeaders.Add("contentType", "application/json");
            var response = await _httpClient.PostAsJsonAsync<ResetDocumentRequestEntity>(uri, new ResetDocumentRequestEntity() { DocumentKeys = [key] }, CancellationToken.None);
            return new ServiceResponse() { IsSuccess = (HttpStatusCode)response.StatusCode == HttpStatusCode.NoContent, Code = (HttpStatusCode)response.StatusCode };
        }

        public async Task<ServiceResponse> DeleteDocumentAsync(string key)
        {
            var response = await _searchClient.DeleteDocumentsAsync("id", new List<string>() { key });
            return new ServiceResponse() { IsSuccess = (HttpStatusCode)response.GetRawResponse().Status == HttpStatusCode.OK, Code = (HttpStatusCode)response.GetRawResponse().Status };
        }

        public async Task<ServiceResponse> RunIndexerAsync(string name)
        {
            if (String.IsNullOrEmpty(name)) { name = _defaultIndexerName; }
            var response = await _indexerClient.RunIndexerAsync(name);
            return new ServiceResponse() { IsSuccess = !response.IsError, Code = (HttpStatusCode)response.Status };
        }

        public async Task<List<string>> GetFailedIndexerRecordsAsync(string name)
        {
            if (String.IsNullOrEmpty(name)) { name = _defaultIndexerName; }

            var response = await _indexerClient.GetIndexerStatusAsync(name);

            var failedHistory = response.Value.ExecutionHistory.Where(x => x.FailedItemCount > 0);

            var failed = new List<string>();
            foreach (var f in failedHistory)
            {
                failed.AddRange(f.Errors.Select(x => GetDocumentKeyValue(x.Key)).Where(key => key != null)!);
                failed.AddRange(f.Warnings.Select(x => GetDocumentKeyValue(x.Key)).Where(key => key != null)!);
            }

            return failed.Distinct().ToList();
        }

        private static string? GetDocumentKeyValue(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var value = input.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .FirstOrDefault(kv => kv.Length == 2 && kv[0].Equals("localid", StringComparison.OrdinalIgnoreCase))?[1];

            return value != null ? Uri.UnescapeDataString(value) : null;
        }

    }

}

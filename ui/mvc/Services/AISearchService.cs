using Azure.Search.Documents.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using static System.Net.WebRequestMethods;
using System.Net;

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
    }

}

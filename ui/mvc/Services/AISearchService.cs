using Azure.Search.Documents.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using static System.Net.WebRequestMethods;

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

        public async Task<Pageable<SearchResult<SearchDocument>>> Query(string filter, string searchString = "*")
        {
            SearchOptions options = new SearchOptions()
            {
                Filter = filter,
                SearchMode = SearchMode.All
            };

            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(searchString, options);

            return response.GetResults();
        }

        public async Task<Pageable<SearchResult<SearchDocument>>> Query(string searchString = "*")
        {
            SearchOptions options = new SearchOptions()
            {
                SearchMode = SearchMode.All
            };

            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(searchString, options);

            return response.GetResults();
        }

        public async Task<bool> ResetDocument(string key)
        {
            // Reset document is in preview.
            var uri = $"{_searchUri}/indexers/{_defaultIndexerName}/resetdocs?api-version=2020-06-30-Preview";
            _httpClient.DefaultRequestHeaders.Remove("api-key");
            _httpClient.DefaultRequestHeaders.Add("api-key", _searchApiKey);
            _httpClient.DefaultRequestHeaders.Remove("contentType");
            _httpClient.DefaultRequestHeaders.Add("contentType", "application/json");
            var response = await _httpClient.PostAsJsonAsync<ResetDocumentRequestEntity>(uri, new ResetDocumentRequestEntity() { DocumentKeys = [key] }, CancellationToken.None);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return true;
            return false;
        }

        public async Task<bool> DeleteDocument(string key)
        {
            var response = await _searchClient.DeleteDocumentsAsync("id", new List<string>() { key });
            return true;
        }

        public async Task<bool> RunIndexer(string name)
        {
            if (String.IsNullOrEmpty(name)) { name = _defaultIndexerName; }
            var response = await _indexerClient.RunIndexerAsync(name);
            if (response.IsError) return false;
            return true;
        }
    }

}

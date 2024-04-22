using Azure.Search.Documents.Models;
using Azure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.Services;

namespace PhiDeidPortal.Ui.PageModels
{
    public class PhiDeidPageModelBase : PageModel
    {
        public Pageable<SearchResult<SearchDocument>> AllDocuments { get { return _allDocuments; } }
        private Pageable<SearchResult<SearchDocument>> _allDocuments;
        public Pageable<SearchResult<SearchDocument>> DocumentSearchResults { get; private set; }
        public List<dynamic> CosmosResults { get { return _cosmosResults; } }
        private List<dynamic> _cosmosResults = new List<dynamic>();

        public int ReviewCount { get; set; }
        public int JustificationCount { get; set; }
        public int CompletedCount { get; set; }
        public int UnprocessedCount { get; set; }
        public int TotalCount { get; set; }
        public int DeniedCount { get; set; }

        private readonly IAISearchService _indexQueryer;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfigurationRoot _configuration;

        public PhiDeidPageModelBase(IAISearchService indexQueryer, CosmosClient cosmosClient, IConfiguration configRoot)
        {
            _indexQueryer = indexQueryer;
            _cosmosClient = cosmosClient;
            _configuration = (IConfigurationRoot)configRoot;
        }
        public async Task DoCounts()
        {
            var cosmosDb = _cosmosClient.GetDatabase("deid");
            var cosmosContainer = cosmosDb.GetContainer("metadata");
            var results = cosmosContainer.GetItemQueryIterator<dynamic>($"SELECT * FROM c where c.Author = '{User.Identity.Name}'");
            _cosmosResults = new List<dynamic>();
            while (results.HasMoreResults)
            {
                FeedResponse<dynamic> response = await results.ReadNextAsync();
                _cosmosResults.AddRange(response);
            }

            TotalCount = _cosmosResults.Count();
            UnprocessedCount = _cosmosResults.Count(x => x.Status == 1);
            JustificationCount = _cosmosResults.Count(x => x.Status == 2);
            ReviewCount = _cosmosResults.Count(x => x.Status == 3);
            CompletedCount = _cosmosResults.Count(x => x.Status == 4);
            DeniedCount = _cosmosResults.Count(x => x.Status == 5);
        }

        public bool IsAuthorized
        {
            get
            {
                var userGroupClaim = User.Claims.FirstOrDefault(c => c.Type == "groups" && c.Value == _configuration.GetValue<string>("GroupClaimAdminId"));
                return userGroupClaim != null;
            }
        }
        public async Task Query(string filter, string searchString)
        {
            DocumentSearchResults = await _indexQueryer.Query(filter, searchString);
        }
    }
}

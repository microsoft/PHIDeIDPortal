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
        public int ApprovedCount { get; set; }
        public int UnprocessedCount { get; set; }
        public int TotalCount { get; set; }
        public int DeniedCount { get; set; }

        private readonly IAISearchService _indexQueryer;
        private readonly CosmosClient _cosmosClient;
        private readonly IAuthorizationService _authService;

        public PhiDeidPageModelBase(IAISearchService indexQueryer, CosmosClient cosmosClient, IAuthorizationService authService)
        {
            _indexQueryer = indexQueryer;
            _cosmosClient = cosmosClient;
            _authService = authService;
        }
        public async Task DoCounts(bool filterByAuthor)
        {
            var cosmosDb = _cosmosClient.GetDatabase("deid");
            var cosmosContainer = cosmosDb.GetContainer("metadata");
            var query = (IsAuthorized && !filterByAuthor) ? $"SELECT * FROM c" : $"SELECT * FROM c where c.Author = '{User.Identity.Name}'";
            var results = cosmosContainer.GetItemQueryIterator<dynamic>(query);
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
            ApprovedCount = _cosmosResults.Count(x => x.Status == 4);
            DeniedCount = _cosmosResults.Count(x => x.Status == 5);
        }

        public bool IsAuthorized => _authService.Authorize(User);
        
        public async Task Query(string filter, string searchString)
        {
            DocumentSearchResults = await _indexQueryer.Query(filter, searchString);
        }
    }
}

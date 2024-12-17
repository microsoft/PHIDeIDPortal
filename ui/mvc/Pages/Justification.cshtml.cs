using Azure.Search.Documents.Models;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.Mvc;
using PhiDeidPortal.Ui.Services;
using PhiDeidPortal.Ui.Entities;
using IAuthorizationService = PhiDeidPortal.Ui.Services.IAuthorizationService;

namespace PhiDeidPortal.Ui.Pages
{
    [Authorize]
    [FeatureGate(Feature.JustificationView)]
    public class JustificationModel(IAuthorizationService authorizationService, IAISearchService searchService, IFeatureService featureService, ICosmosService cosmosService) : PageModel
    {
        private readonly IAISearchService _searchService = searchService;
        private readonly IAuthorizationService _authService = authorizationService;
        private readonly IFeatureService _featureService = featureService;
        private readonly ICosmosService _cosmosService = cosmosService;

        public Pageable<SearchResult<SearchDocument>>? Results { get; private set; }
        public bool IsDeleteFeatureAvailable { get; private set; }
        public bool IsDownloadFeatureAvailable { get; private set; }

        public void OnGet()
        {
            if (User.Identity?.Name is null) return;
            var viewFilter = Request.Query["v"].ToString().ToLower() == "me";
            var searchString = Request.Query["q"].ToString();
            var isElevated = _authService.HasElevatedRights(User);
            var searchFilter = $"status eq 2";

            Results = (isElevated && !viewFilter) ? _searchService.SearchAsync(searchFilter, searchString).Result : _searchService.SearchByAuthorAsync(User.Identity.Name, searchFilter, searchString).Result;

            IsDeleteFeatureAvailable = _featureService.IsFeatureEnabled(Feature.Delete);
            IsDownloadFeatureAvailable = _featureService.IsFeatureEnabled(Feature.Download);
        }
        
        public async Task<(string, bool)> GetMetadataRecord(string uri)
        {
            var document = _cosmosService.GetMetadataRecordByUri(uri);
            return document is null ? ("No justification provided.", document.AwaitingIndex) : (document.JustificationText, document.AwaitingIndex);
        }
    }
}

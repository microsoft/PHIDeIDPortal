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
    [FeatureGate(Feature.ManualReviewView)]
    public class ReviewModel(IAuthorizationService authorizationService, IAISearchService searchService, ICosmosService cosmosService, IFeatureService featureService) : PageModel
    {
        private readonly IAISearchService _searchService = searchService;
        private readonly IAuthorizationService _authService = authorizationService;
        private readonly ICosmosService _cosmosService = cosmosService;
        private readonly IFeatureService _featureService = featureService;
        public Pageable<SearchResult<SearchDocument>>? Results { get; private set; }
        public bool UserHasElevatedRights { get; set; }
        public bool IsDownloadFeatureAvailable { get; private set; }

        public void OnGet()
        {
            if (User.Identity?.Name is null) return;
            var viewFilter = Request.Query["v"].ToString().ToLower() == "me";
            var searchString = Request.Query["q"].ToString();
            var isElevated = _authService.HasElevatedRights(User);
            var searchFilter = $"status eq 3";

            Results = (isElevated && !viewFilter) ? _searchService.SearchAsync(searchFilter, searchString).Result : _searchService.SearchByAuthorAsync(User.Identity.Name, searchFilter, searchString).Result;
            UserHasElevatedRights = isElevated;
            IsDownloadFeatureAvailable = _featureService.IsFeatureEnabled(Feature.Download);
        }

        public async Task<string> GetJustificationText(string uri)
        {
            var document = _cosmosService.GetMetadataRecordByUri(uri);
            return document is null ? "No justification provided." : document.JustificationText;
        }
    }
}


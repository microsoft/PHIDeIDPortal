using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.Mvc;
using PhiDeidPortal.Ui.Services;
using PhiDeidPortal.Ui.Entities;
using IAuthorizationService = PhiDeidPortal.Ui.Services.IAuthorizationService;

namespace PhiDeidPortal.Ui.Pages
{
    [Authorize]
    [FeatureGate(Feature.AllDocumentsView)]
    public class IndexModel(IAuthorizationService authorizationService, ICosmosService cosmosService, IAISearchService searchService, IFeatureService featureService, IConfiguration configuration) : PageModel
    {
        private readonly IAISearchService _searchService = searchService;
        private readonly IAuthorizationService _authService = authorizationService;
        private readonly ICosmosService _cosmosService = cosmosService;
        private readonly IFeatureService _featureService = featureService;

        public List<string> FailedRecords { get; private set; } = [];
        public bool IsDeleteFeatureAvailable { get; private set; }
        public List<MetadataRecord> Results { get; private set; } = [];

        public void OnGet()
        {
            if (User.Identity?.Name is null) return;
            var viewFilter = Request.Query["v"].ToString().ToLower() == "me";
            var searchString = Request.Query["q"].ToString();
            var isElevated = _authService.HasElevatedRights(User);
            FailedRecords = _searchService.GetFailedIndexerRecordsAsync(String.Empty).Result;
            IsDeleteFeatureAvailable = _featureService.IsFeatureEnabled(Feature.Delete);
            Results = (isElevated && !viewFilter) ? _cosmosService.GetAllMetadataRecords(searchString) : _cosmosService.GetAllMetadataRecordsByAuthor(User.Identity.Name, searchString);
        }
    }
}

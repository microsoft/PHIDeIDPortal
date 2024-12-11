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
    public class IndexModel(IAuthorizationService authorizationService, ICosmosService cosmosService) : PageModel
    {
        private readonly ICosmosService _cosmosService = cosmosService;
        private readonly IAuthorizationService _authService = authorizationService;
        public List<MetadataRecord> Results { get; private set; } = [];

        public void OnGet()
        {
            if (User.Identity?.Name is null) return;
            var viewFilter = Request.Query["v"].ToString().ToLower() == "me";
            var isElevated = _authService.HasElevatedRights(User);
            Results = (isElevated && !viewFilter) ? _cosmosService.GetAllMetadataRecords() : _cosmosService.GetAllMetadataRecordsByAuthor(User.Identity.Name);
        }
    }
}

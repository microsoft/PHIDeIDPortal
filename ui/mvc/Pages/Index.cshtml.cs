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
            var searchString = Request.Query["q"].ToString();
            var isElevated = _authService.HasElevatedRights(User);

            var fieldValues = new List<CosmosFieldQueryValue>();

            if (searchString is not null && searchString.Length > 0)
            {
                fieldValues.Add(new CosmosFieldQueryValue() { FieldName = "Uri", FieldValue = searchString, IsPrefixMatch = true });
                fieldValues.Add(new CosmosFieldQueryValue() { FieldName = "Author", FieldValue = searchString, IsPrefixMatch = true });
                fieldValues.Add(new CosmosFieldQueryValue() { FieldName = "FileName", FieldValue = searchString, IsPrefixMatch = true });
                fieldValues.Add(new CosmosFieldQueryValue() { FieldName = "Status", FieldValue = EnumExtensions.GetDeidStatusValueFromPrefix(searchString) });
            }

            if (!isElevated || viewFilter)
            {
                fieldValues.Add(new CosmosFieldQueryValue() { FieldName = "Author", FieldValue = User.Identity.Name, IsRequired = true });
            }

            Results = _cosmosService.QueryMetadataRecords(fieldValues).Result;

        }
    }
}

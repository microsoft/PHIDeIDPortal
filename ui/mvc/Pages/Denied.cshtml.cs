using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;
using PhiDeidPortal.Ui.Services;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.PageModels;

namespace PhiDeidPortal.Ui.Pages
{
    [Authorize]
    public class DeniedModel : PhiDeidPageModelBase
    {
        private readonly ILogger<DeniedModel> _logger;

        public DeniedModel(ILogger<DeniedModel> logger, IAISearchService indexQueryer, CosmosClient cosmosClient, Services.IAuthorizationService authorizationService)
            : base(indexQueryer, cosmosClient, authorizationService)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            var viewQuery = Request.Query["v"].ToString();
            var searchQuery = Request.Query["q"].ToString();

            await base.DoCounts(viewQuery.ToLower() == "me");

            var filter = $"status eq 5";
            var searchString = searchQuery ?? "*";
            if (viewQuery.ToLower() == "me" || !IsAuthorized) { searchString += $"+{User.Identity.Name}"; }
            await Query(filter, searchString);
        }
    }
}

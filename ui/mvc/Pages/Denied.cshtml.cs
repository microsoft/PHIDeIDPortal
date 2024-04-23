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
            await base.DoCounts();

            var filter = $"status eq 5";

            var searchString = Request.Query["q"].ToString() ?? "*";

            await Query(filter, searchString);
        }
    }
}

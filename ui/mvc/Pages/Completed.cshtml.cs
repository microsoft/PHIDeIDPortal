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
    public class CompletedModel : PhiDeidPageModelBase
    {
        private readonly ILogger<CompletedModel> _logger;

        public CompletedModel(ILogger<CompletedModel> logger, IAISearchService indexQueryer, CosmosClient cosmosClient, Services.IAuthorizationService authorizationService)
            : base(indexQueryer, cosmosClient, authorizationService)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            await base.DoCounts();

            var filter = $"status eq 4";

            var searchString = Request.Query["q"].ToString() ?? "*";

            await Query(filter, searchString);
        }
    }
}

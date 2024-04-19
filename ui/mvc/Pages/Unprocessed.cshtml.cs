using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.PageModels;
using PhiDeidPortal.Ui.Services;

namespace PhiDeidPortal.Ui.Pages
{
    [Authorize]
    public class UnprocessedModel : PhiDeidPageModelBase
    {
        private readonly ILogger<UnprocessedModel> _logger;

        public UnprocessedModel(ILogger<UnprocessedModel> logger, IAISearchService indexQueryer, CosmosClient cosmosClient, IConfiguration configRoot)
            : base(indexQueryer, cosmosClient, configRoot)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            await base.DoCounts();
        }
    }
}

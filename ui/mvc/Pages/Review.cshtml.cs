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
    public class ReviewModel : PhiDeidPageModelBase
    {
        private readonly ILogger<ReviewModel> _logger;
        private readonly ICosmosService _cosmosService;

        public ReviewModel(ILogger<ReviewModel> logger, IAISearchService indexQueryer, CosmosClient cosmosClient, IConfiguration configRoot, Services.IAuthorizationService authorizationService)
            : base(indexQueryer, cosmosClient, authorizationService)
        {
            _logger = logger;
            _cosmosService = new CosmosService(cosmosClient, configRoot);
        }

        public async Task OnGet()
        {
            if (!IsAuthorized) return;

            await base.DoCounts();

            var filter = $"status eq 3";
            var searchString = Request.Query["q"].ToString() ?? "*";

            await Query(filter, searchString);
        }

        public async Task<string> GetJustificationText(string uri)
        {
            var document = _cosmosService.GetMetadataRecordByUri(uri);

            if (null != document)
            { 
                return document.JustificationText;
            }

            return "No justification provided.";
        }
    }
}

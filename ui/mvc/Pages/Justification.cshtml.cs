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
    public class JustificationModel : PhiDeidPageModelBase
    {
        private readonly IBlobService _blobService;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<JustificationModel> _logger;
        private readonly IConfigurationRoot _configuration;

        private string _cosmosDbName = "";
        private string _cosmosContainerName = "";

        public JustificationModel(ILogger<JustificationModel> logger, IAISearchService indexQueryer, CosmosClient cosmosClient, IConfiguration configRoot, IBlobService blobService)
            : base(indexQueryer, cosmosClient, configRoot)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            var _cosmosConfig = configRoot.GetSection("CosmosDb");
            _cosmosDbName = _cosmosConfig["DatabaseId"];
            _cosmosContainerName = _cosmosConfig["ContainerId"];
            _blobService = blobService;
        }

        public async Task OnGet()
        {
            await base.DoCounts();

            var filter = $"status eq 2";

            var searchString = Request.Query["q"].ToString() ?? "*";

            await Query(filter, searchString);
        }

        public async Task<string> GetSasUri(string docId)
        {
            var docRecord = _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .GetItemLinqQueryable<MetadataRecord>(true)
                .Where(d => d.id == docId)
                .FirstOrDefault();

            Uri uri;
            string uriString = "";

            if (null != docRecord)
            {
                uri = await _blobService.GetSasUri(_cosmosContainerName, docRecord.FileName);

                uriString = uri.ToString() ?? "";
            }   

            return uriString;
        }
    }
}

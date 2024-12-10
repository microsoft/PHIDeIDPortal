using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.Controllers;
using PhiDeidPortal.Ui.Services;
using PhiDeidPortal.Ui.ViewComponents;
using System.Security.Claims;
using IAuthorizationService = PhiDeidPortal.Ui.Services.IAuthorizationService;

namespace PhiDeidPortal.Ui.Hubs
{    
    public class CosmosDocuments : Hub
    {
        private readonly ICosmosService _cosmosService;
        private readonly IAuthorizationService _authService;
        private readonly IAISearchService _indexQueryer;
        private readonly CosmosClient _cosmosClient;
        private readonly IFeatureService _featureService;
        private readonly IUserContextService _userContextService;

        public CosmosDocuments(ICosmosService cosmosService, IAuthorizationService authService, IAISearchService indexQueryer, CosmosClient cosmosClient, IFeatureService featureService, IUserContextService userContextService)
        {
            _authService = authService;
            _cosmosService = cosmosService;
            _indexQueryer = indexQueryer;
            _cosmosClient = cosmosClient;
            _featureService = featureService;
            _userContextService = userContextService;
        }

        public async Task UpdateCounts()
        {
            var currentUser = _userContextService.User;
            if (currentUser == null)
            {
                // Handle the case where the user is not authenticated
                await Clients.Caller.SendAsync("Error", "User is not authenticated.");
                return;
            }

            var isElevated = _userContextService.HasElevatedRights;
            var viewFilter = _userContextService.ViewFilter;

            var summary = (isElevated && !viewFilter) ? _cosmosService.GetSummary() : _cosmosService.GetSummaryByAuthor(currentUser.Identity.Name);
            await Clients.All.SendAsync("UpdateCounts", summary);
        }
    }

    public class CosmosDbDocument
    {
        public string User { get; set; }
        public string Message { get; set; }
    }

    public interface IUserContextService
    {
        ClaimsPrincipal User { get; set; }
        bool HasElevatedRights { get; set; }
        bool ViewFilter { get; set; }
    }

    public class UserContextService : IUserContextService
    {
        public ClaimsPrincipal User { get; set; }
        public bool HasElevatedRights { get; set; }
        public bool ViewFilter { get; set; }
    }
}
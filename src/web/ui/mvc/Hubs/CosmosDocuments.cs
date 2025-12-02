using Microsoft.AspNetCore.SignalR;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;

namespace PhiDeidPortal.Ui.Hubs
{
    public class CosmosDocuments : Hub
    {
        private readonly ICosmosService _cosmosService;
        private readonly IUserContextService _userContextService;

        public CosmosDocuments(ICosmosService cosmosService, IUserContextService userContextService)
        {
            _cosmosService = cosmosService;
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
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Hubs;
using PhiDeidPortal.Ui.Interfaces;
using PhiDeidPortal.Ui.Services;
using System.Security.Claims;

namespace PhiDeidPortal.Ui.ViewComponents
{
    public class TabbedNavigationViewComponent : ViewComponent
    {
        private readonly IAISearchService _indexQueryer;
        private readonly CosmosClient _cosmosClient;
        private readonly ICosmosService _cosmosService;
        private readonly IAuthorizationService _authService;
        private readonly IFeatureService _featureService;
        private readonly IUserContextService _userContextService;

        public TabbedNavigationViewComponent(IAISearchService indexQueryer, CosmosClient cosmosClient, IAuthorizationService authService, ICosmosService cosmosService, IFeatureService featureService, IUserContextService userContextService)
        {
            _indexQueryer = indexQueryer;
            _cosmosClient = cosmosClient;
            _authService = authService;
            _cosmosService = cosmosService;
            _featureService = featureService;
            _userContextService = userContextService;
        }

        public StatusSummary GetSummary()
        {
            var isElevated = _authService.HasElevatedRights((ClaimsPrincipal)User);
            var viewFilter = Request.Query["v"].ToString().ToLower() == "me";

            _userContextService.User = (ClaimsPrincipal)User;
            _userContextService.HasElevatedRights = isElevated;
            _userContextService.ViewFilter = viewFilter;

            return (isElevated && !viewFilter) ? _cosmosService.GetSummary() : _cosmosService.GetSummaryByAuthor(User.Identity.Name);
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_featureService.IsFeatureEnabled(Feature.TabbedNavigation))
                return View(new TabbedNavigationViewModel() { IsFeatureAvailable = false });

            if (User.Identity?.Name is null) 
                return View(new TabbedNavigationViewModel() { IsFeatureAvailable = false });            
                        

            var viewModel = new TabbedNavigationViewModel()
            {
                IsFeatureAvailable = true,
                StatusSummary = GetSummary()                
            };

            viewModel.PageFeatures.Add(Feature.AllDocumentsView, _featureService.IsFeatureEnabled(Feature.AllDocumentsView));
            viewModel.PageFeatures.Add(Feature.UnprocessedView, _featureService.IsFeatureEnabled(Feature.UnprocessedView));
            viewModel.PageFeatures.Add(Feature.JustificationView, _featureService.IsFeatureEnabled(Feature.JustificationView));
            viewModel.PageFeatures.Add(Feature.ManualReviewView, _featureService.IsFeatureEnabled(Feature.ManualReviewView));
            viewModel.PageFeatures.Add(Feature.ApprovedView, _featureService.IsFeatureEnabled(Feature.ApprovedView));
            viewModel.PageFeatures.Add(Feature.DeniedView, _featureService.IsFeatureEnabled(Feature.DeniedView));
            viewModel.PageFeatures.Add(Feature.Upload, _featureService.IsFeatureEnabled(Feature.Upload));
            viewModel.PageFeatures.Add(Feature.Search, _featureService.IsFeatureEnabled(Feature.Search));

            return View(viewModel);
        }

    }


}

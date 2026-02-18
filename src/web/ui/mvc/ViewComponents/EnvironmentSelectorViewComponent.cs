using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;
using System.Text;
using System.Text.Json;

namespace PhiDeidPortal.Ui.ViewComponents;

public class EnvironmentSelectorViewComponent : ViewComponent
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;

    public EnvironmentSelectorViewComponent(
        IAuthorizationService authorizationService, 
        IConfiguration configuration,
        IDistributedCache cache)
    {
        _authorizationService = authorizationService;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var authorizedEnvironments = _authorizationService.GetAuthorizedEnvironments(UserClaimsPrincipal);
        HttpContext.Items["AuthorizedEnvironmentCount"] = authorizedEnvironments.Count;
        if (authorizedEnvironments.Count == 0) return View(new EnvironmentSelectorViewModel());

        var defaultEnvironment = authorizedEnvironments.OrderByDescending(x => x.IsDefault).First();

        var model = new EnvironmentSelectorViewModel
        {
            DefaultEnvironment = defaultEnvironment,
            AuthorizedEnvironments = authorizedEnvironments
        };

        model.CurrentEnvironment = defaultEnvironment.EnvironmentName;

        return View(model);
    }
}

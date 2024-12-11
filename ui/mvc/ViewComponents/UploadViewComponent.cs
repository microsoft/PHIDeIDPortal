using Microsoft.AspNetCore.Mvc;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;

namespace PhiDeidPortal.Ui.ViewComponents
{
    public class UploadViewComponent : ViewComponent
    {
        private readonly IFeatureService _featureService;

        public UploadViewComponent(IFeatureService featureService)
        {
            _featureService = featureService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_featureService.IsFeatureEnabled(Feature.Upload))
                return View(new UploadViewModel() { IsFeatureAvailable = false });

            return View(new UploadViewModel() { IsFeatureAvailable = true });
        }
     }
}

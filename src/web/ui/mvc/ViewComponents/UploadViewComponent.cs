using Microsoft.AspNetCore.Mvc;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;

namespace PhiDeidPortal.Ui.ViewComponents
{
    public class UploadViewComponent : ViewComponent
    {
        private readonly IFeatureService _featureService;
        private readonly IConfiguration _configuration;

        public UploadViewComponent(IFeatureService featureService, IConfiguration configuration)
        {
            _featureService = featureService;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_featureService.IsFeatureEnabled(Feature.Upload))
                return View(new UploadViewModel() { IsFeatureAvailable = false });

            var configuration = _configuration.GetSection("Kestrel");
            var maxRequestBodySize = configuration["MaxRequestBodySizeinMB"];
            maxRequestBodySize ??= "100";
            var size = int.Parse(maxRequestBodySize);

            return View(new UploadViewModel() { IsFeatureAvailable = true, MaxFileSize = size });
        }
     }
}

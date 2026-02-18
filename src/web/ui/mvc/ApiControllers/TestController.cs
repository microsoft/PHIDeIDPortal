using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace PhiDeidPortal.Ui.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IFeatureManager _featureManager;

    public TestController(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    [HttpGet("check-feature/{featureName}")]
    public async Task<IActionResult> CheckFeature(string featureName)
    {
        var isEnabled = await _featureManager.IsEnabledAsync(featureName);
        return Ok(new
        {
            Feature = featureName,
            IsEnabled = isEnabled,
            User = User?.Identity?.Name,
            IsAuthenticated = User?.Identity?.IsAuthenticated
        });
    }
}
using Microsoft.FeatureManagement;

namespace PhiDeidPortal.Ui.Services
{
    public class FeatureService : IFeatureService
    {
        private readonly IFeatureManagerSnapshot _featureManager;

        public FeatureService(IFeatureManagerSnapshot featureManager)
        {
            _featureManager = featureManager;
        }

        public async Task<bool> IsFeatureEnabledAsync(string featureName)
        {
            return await _featureManager.IsEnabledAsync(featureName);
        }
    }
}

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

        public bool IsFeatureEnabled(string featureName)
        {
            return _featureManager.IsEnabledAsync(featureName).GetAwaiter().GetResult();
        }
    }
}

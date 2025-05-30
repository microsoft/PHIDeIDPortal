using Microsoft.FeatureManagement;

namespace PhiDeidPortal.Ui.Services
{
    public class FeatureService(IFeatureManager featureManager) : IFeatureService
    {
        private readonly IFeatureManager _featureManager = featureManager;

        public bool IsFeatureEnabled(string featureName)
        {
            return _featureManager.IsEnabledAsync(featureName).Result;
        }
    }
}

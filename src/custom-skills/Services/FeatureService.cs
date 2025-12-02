using Newtonsoft.Json;
using PhiDeidPortal.CustomFunctions.Entities;

namespace PhiDeidPortal.CustomFunctions.Services
{
    internal class FeatureService
    {
        public bool IsFeatureEnabled(string environment, string featureName)
        {
            var features = GetEnvironmentFeatures();
            var environmentFeatures = features.FirstOrDefault(f => f.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase));
            return environmentFeatures?.Features.Contains(featureName, StringComparer.OrdinalIgnoreCase) ?? false;
        }

        public static List<EnvironmentFeatures> GetEnvironmentFeatures()
        {
            var configuration = Environment.GetEnvironmentVariable(EnvironmentVariables.EnvironmentFeatures);
            return configuration is null ? [] : JsonConvert.DeserializeObject<List<EnvironmentFeatures>>(configuration);
        }
    }

    public class EnvironmentFeatures
    {
        public string Environment { get; set; }
        public List<string> Features { get; set; }
    }

}

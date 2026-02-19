namespace PhiDeidPortal.Ui.Services
{
    public interface IFeatureService
    {
        Task<bool> IsFeatureEnabledAsync(string featureName);
    }
}

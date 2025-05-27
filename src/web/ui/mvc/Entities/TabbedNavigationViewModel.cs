namespace PhiDeidPortal.Ui.Entities
{
    public class TabbedNavigationViewModel
    {
        public bool IsAuthorized { get; set; }
        public bool IsFeatureAvailable { get; set; }
        public Dictionary<string, bool> PageFeatures { get; set; } = new Dictionary<string, bool>();
        public StatusSummary StatusSummary { get; set; } = new StatusSummary();
    }
}

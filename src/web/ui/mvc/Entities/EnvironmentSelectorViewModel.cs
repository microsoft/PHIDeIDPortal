namespace PhiDeidPortal.Ui.Entities;

public class EnvironmentSelectorViewModel
{
    public string CurrentEnvironment { get; set; } = string.Empty;
    public EnvironmentGroupClaim DefaultEnvironment { get; set; } = new EnvironmentGroupClaim();
    public List<EnvironmentGroupClaim> AuthorizedEnvironments { get; set; } = [];
}

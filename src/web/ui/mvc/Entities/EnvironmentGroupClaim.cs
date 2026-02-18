namespace PhiDeidPortal.Ui.Entities;

public class EnvironmentGroupClaim
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string[] GroupClaimIds { get; set; } = [];
    public bool IsDefault { get; set; }
    public Dictionary<string, bool> Features { get; set; } = new();
}

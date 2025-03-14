namespace PhiDeidPortal.Ui.Entities
{
    public enum DeidStatus
    {
        Uploaded = 1,
        RequiresJustification = 2,
        JustificationApprovalPending = 3,
        Approved = 4,
        Denied = 5
    }

    public static class EnumExtensions
    {
        public static int GetDeidStatusValueFromPrefix(string prefix)
        {
            var match = Enum.GetValues(typeof(DeidStatus))
                            .Cast<DeidStatus>()
                            .FirstOrDefault(e => e.ToString().IndexOf(prefix, StringComparison.OrdinalIgnoreCase) == 0);

            return match == default ? 0 : (int)match;
        }
    }
}

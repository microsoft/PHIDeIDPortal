namespace PhiDeidPortal.Ui.Entities
{
    public class CosmosFieldQueryValue
    {
        public string FieldName { get; set; } = string.Empty;
        required public object FieldValue { get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsPrefixMatch { get; set; } = false;
    }
}

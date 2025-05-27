namespace PhiDeidPortal.Ui.Entities
{
    public class CosmosRecord
    {
        public string id { get; set; }
        public string Uri { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public string Author { get; set; }
        public bool AwaitingIndex { get; set; }
        public string JustificationText { get; set; }
        public DateTime LastIndexed { get; set; }
        public string[] OrganizationalMetadata { get; set; }
    }
}

using CosmosDBMonitor;
using System.Text.Json.Serialization;
using custom_skills.Utilities;

namespace custom_skills.Models
{
    public class CosmosRecordRaw
    {
        public string id { get; set; }
        public string Uri { get; set; }
        public string FileName { get; set; }
        [JsonConverter(typeof(StatusConverter))]
        public string Status { get; set; }
        public string Author { get; set; }
        public bool AwaitingIndex { get; set; }
        public string JustificationText { get; set; }
        public DateTime LastIndexed { get; set; }
        public string[] OrganizationalMetadata { get; set; }
        public string _rid { get; set; }
        public string _self { get; set; }
        public string _etag { get; set; }
        public string _attachments { get; set; }
        public int _ts { get; set; }
        public int _lsn { get; set; }
    }
}

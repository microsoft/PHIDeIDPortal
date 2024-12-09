using Microsoft.AspNetCore.SignalR;

namespace PhiDeidPortal.Ui.Hubs
{
    public class CosmosDocuments : Hub
    {
        public async Task SendMessage(CosmosDbDocument doc)
        {
            await Clients.All.SendAsync("ReceiveDocument", doc.User, doc.Message);            
        }
        public async Task UpdateCounts(CosmosDbDocument doc)
        {            
            await Clients.All.SendAsync("UpdateCounts", new StatusSummary());
        }
    }

    public class CosmosDbDocument
    {
        public string User { get; set; }
        public string Message { get; set; }
    }

    public class StatusSummary
    {
        public int TotalCount { get; set; } = 1;
        public int UnprocessedCount { get; set; } = 2;
        public int JustificationCount { get; set; } = 3;
        public int ReviewCount { get; set; } = 4;
    }
}
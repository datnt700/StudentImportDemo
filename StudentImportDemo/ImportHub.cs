using Microsoft.AspNetCore.SignalR;

namespace StudentImportDemo
{
    public class ImportHub : Hub
    {
        public Task JoinImport(string importId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, importId);
        }
    }
}

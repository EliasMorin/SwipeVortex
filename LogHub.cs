using Microsoft.AspNetCore.SignalR;

namespace SwipeVortexWb;

public class LogHub : Hub
{
    public async Task SendLog(string message, string logLevel)
    {
        await Clients.All.SendAsync("ReceiveLog", message, logLevel);
    }
}
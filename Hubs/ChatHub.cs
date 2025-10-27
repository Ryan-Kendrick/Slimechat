using Microsoft.AspNetCore.SignalR;

namespace Hubs;

public class ChatHub : Hub
{
    public async Task BroadcastMessage(long username, string message) => await Clients.All.SendAsync("MessageReceived", username, message);

    public async Task SendAsServer(long username, string message)
        => await Clients.All.SendAsync("ServerMessage", username, message);

    // Send message ack to clients
    // Send user joined / left events

}
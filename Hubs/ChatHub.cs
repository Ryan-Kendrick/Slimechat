using Microsoft.AspNetCore.SignalR;
using Models.Slimechat;

namespace Hubs;


public class ChatHub : Hub
{
    // Debug-----
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }
        await base.OnDisconnectedAsync(exception);
    }
    //-----------
    public async Task BroadcastMessage(MessageData messageData)
    {
        _logger.LogInformation($"BroadcastMessage: {messageData.Name} said: {messageData.Content} at {DateTimeOffset.UtcNow}");
        await Clients.All.SendAsync("MessageReceived", new
        {
            name = messageData.Name,
            color = messageData.Color,
            content = messageData.Content,
            unixTime = messageData.unixTime,
            id = $"{messageData.Name}.{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        });
    }


    public async Task SendAsServer(long username, string message)
    {
        _logger.LogInformation($"SendAsServer called: user={username}, message={message}");
        await Clients.All.SendAsync("ServerMessage", username, message);
    }
}
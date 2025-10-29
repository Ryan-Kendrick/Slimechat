using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Hubs;


public class ChatHub : Hub
{
    // Debug----------------------------------------------
    private readonly ILogger<ChatHub> _logger;
    private readonly ChatSettings _chatSettings;

    public ChatHub(ILogger<ChatHub> logger, IOptions<ChatSettings> chatSettings)
    {
        _logger = logger;
        _chatSettings = chatSettings.Value;
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
    //----------------------------------------------------

    public async Task BroadcastMessage(MessageData messageData)
    {
        messageData.Name = SanitiseName(messageData.Name);
        messageData.Color = SanitiseColor(messageData.Color);
        messageData.Content = SanitiseContent(messageData.Content);

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

    // Utils
    private string SanitiseColor(string? hexcolor) => Regex.IsMatch(hexcolor ?? "", @"^#[0-9A-Fa-f]{6}$") ? hexcolor! : "#000000";

    private string SanitiseName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Slime" : name[..Math.Min(_chatSettings.NameLengthMax, name.Length)];

    private string SanitiseContent(string? messageContent) =>
        string.IsNullOrWhiteSpace(messageContent) ? "" : messageContent[..Math.Min(_chatSettings.MessageLengthMax, messageContent.Length)];
}
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly ChatSettings _chatSettings;

    public ChatHub(ILogger<ChatHub> logger, IOptions<ChatSettings> chatSettings)
    {
        _logger = logger;
        _chatSettings = chatSettings.Value;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($@"
Client connected
    Connection ID: {Context.ConnectionId}
    User Identifier: {Context.UserIdentifier}
    Remote IP: {Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString()}
        ");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {


        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }
        else
        {
            _logger.LogInformation("Client disconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task BroadcastMessage(MessageData messageData)
    {
        var sanitised = new
        {
            name = SanitiseName(messageData.Name),
            color = SanitiseColor(messageData.Color),
            content = SanitiseContent(messageData.Content),
            unixTime = messageData.unixTime,
            id = $"{messageData.Name}." +
                $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        };

        try
        {
            _logger.LogInformation(
                "BroadcastMessage from {Name} at {UtcTime}: {Content}",
                sanitised.name,
                DateTimeOffset.UtcNow,
                sanitised.content
            );

            await Clients.All.SendAsync(
                "MessageReceived",
                sanitised,
                Context.ConnectionAborted
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Broadcast canceled (connection aborted or token canceled)"
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BroadcastMessage failed");
            throw new HubException("Failed to broadcast the message.");
        }
    }

    public async Task SendAsServer(long username, string message)
    {
        try
        {
            _logger.LogInformation(
                "SendAsServer invoked for {User}: {Message}",
                username,
                message
            );

            await Clients.All.SendAsync(
                "ServerMessage",
                username,
                message,
                Context.ConnectionAborted
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SendAsServer canceled (connection aborted)");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendAsServer failed");
            throw new HubException("Failed to send server message.");
        }
    }

    private string SanitiseColor(string? hexcolor) =>
        Regex.IsMatch(hexcolor ?? "", @"^#[0-9A-Fa-f]{6}$")
            ? hexcolor!
            : "#000000";

    private string SanitiseName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? "Slime"
            : name[..Math.Min(_chatSettings.NameLengthMax, name.Length)];

    private string SanitiseContent(string? content) =>
        string.IsNullOrWhiteSpace(content)
            ? ""
            : content[..Math.Min(_chatSettings.MessageLengthMax, content.Length)];
}
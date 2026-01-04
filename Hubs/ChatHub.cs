using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Hubs;

public class ChatHub(ILogger<ChatHub> logger, IOptions<ChatSettings> chatSettings, ChatDb chatDb) : Hub
{
    private readonly ILogger<ChatHub> _logger = logger;
    private readonly ChatSettings _chatSettings = chatSettings.Value;
    private readonly ChatDb db = chatDb;
    private static readonly Dictionary<string, Queue<DateTime>> _rateLimits = new();

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($@"
Client connected
    Connection ID: {Context.ConnectionId}
    Remote IP: {Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "Unknown"}
        ");

        var recentMessages = await db.Messages.OrderByDescending(m => m.UnixTime).Take(_chatSettings.OnJoinMessageHistoryMax).Reverse().ToListAsync();

        await Clients.Caller.SendAsync("GetMessageHistory", recentMessages);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        _rateLimits.Remove(Context.ConnectionId);

        if (exception != null) _logger.LogError(exception, "Client disconnected with error");

        var leavingUser = await db.ActiveConnections.FirstOrDefaultAsync(conn => conn.ConnectionId == Context.ConnectionId);
        _logger.LogInformation($"User leaving: {leavingUser?.Name ?? "Unknown"}");

        if (leavingUser != null)
        {
            _logger.LogInformation($"Disconnected user: {leavingUser.Name}");
            db.ActiveConnections.Remove(leavingUser);
            await db.SaveChangesAsync();

            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserLeft", leavingUser);

            var connectionsNow = await db.ActiveConnections.ToListAsync();
            // Get a new list of current users; exclude the ConnectionId 
            var activeUsers = connectionsNow
        .Select(u => new ChatUser { Name = u.Name, Id = u.Id, Color = u.Color })
        .ToList();
            await Clients.All.SendAsync("GetActiveUsers", activeUsers);
        }
        else
        {
            _logger.LogWarning($"Connection {Context.ConnectionId} not found in database on disconnect.");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(ChatUser user)
    {
        var connection = new ActiveConnection
        {
            ConnectionId = Context.ConnectionId,
            Id = user.Id,
            Name = user.Name,
            Color = user.Color
        };

        db.ActiveConnections.Add(connection);
        await db.SaveChangesAsync();
        await Clients.AllExcept(Context.ConnectionId).SendAsync("UserJoined", user);

        var connectionsNow = await db.ActiveConnections.ToListAsync();
        var activeUsers = connectionsNow.Select(conn => new ChatUser { Id = conn.Id, Name = conn.Name, Color = conn.Color }).ToList();
        await Clients.Caller.SendAsync("GetActiveUsers", activeUsers);
    }

    public async Task BroadcastMessage(MessageData messageData)
    {
        bool rateLimited = !CheckRateLimit(Context.ConnectionId);

        if (rateLimited) throw new HubException("Rate limit exceeded");

        var sanitisedMessage = new Message
        {
            Id = $"{messageData.Name}." +
                $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            UserId = messageData.UserId,
            Name = SanitiseName(messageData.Name),
            Color = SanitiseColor(messageData.Color),
            Content = SanitiseContent(messageData.Content),
            UnixTime = messageData.UnixTime,
            Type = "user"
        };

        try
        {
            _logger.LogInformation($"BroadcastMessage from {sanitisedMessage.Name} at {DateTimeOffset.UtcNow}: {sanitisedMessage.Content}");
            db.Messages.Add(sanitisedMessage);
            await db.SaveChangesAsync();
            await Clients.All.SendAsync("MessageReceived", sanitisedMessage, Context.ConnectionAborted);
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

    private bool CheckRateLimit(string ConnectionId)
    {
        // If unique names are added this could use user.Name instead for a more robust approach
        if (!_rateLimits.ContainsKey(ConnectionId)) _rateLimits[ConnectionId] = new Queue<DateTime>();

        var queue = _rateLimits[ConnectionId];
        var now = DateTime.UtcNow;

        while (queue.Count > 0 && queue.Peek() < now.AddMinutes(-1)) queue.Dequeue();

        if (queue.Count >= _chatSettings.RateLimitPerMinute) return false;

        queue.Enqueue(now);
        return true;
    }
}
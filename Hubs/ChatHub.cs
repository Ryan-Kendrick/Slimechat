using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly ChatSettings _chatSettings;
    private readonly ChatDb db;
    private static readonly Dictionary<string, Queue<DateTime>> _rateLimits = new();
    public ChatHub(ILogger<ChatHub> logger, IOptions<ChatSettings> chatSettings, ChatDb chatDb)
    {
        _logger = logger;
        _chatSettings = chatSettings.Value;
        db = chatDb;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($@"
Client connected
    Connection ID: {Context.ConnectionId}
    Remote IP: {Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "Unknown"}
        ");

        var recentMessages = await db.Messages.OrderByDescending(m => m.UnixTime).Take(_chatSettings.MessageHistoryMax).Reverse().ToListAsync();

        await Clients.Caller.SendAsync("GetMessageHistory", recentMessages);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        _rateLimits.Remove(Context.ConnectionId);

        if (exception != null) _logger.LogError(exception, "Client disconnected with error");

        var leavingUser = await db.ActiveConnections.FirstOrDefaultAsync(conn => conn.ConnectionId == Context.ConnectionId);

        if (leavingUser != null)
        {
            db.ActiveConnections.Remove(leavingUser);

            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserLeft");

            var connectionsNow = await db.ActiveConnections.ToListAsync();
            var activeUsers = connectionsNow
        .Select(u => new ChatUser { Name = u.Name, Color = u.Color })
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
            Name = user.Name,
            Color = user.Color
        };

        db.Add(connection);
        await db.SaveChangesAsync();
        await Clients.AllExcept(Context.ConnectionId).SendAsync("UserJoined", user);

        var connectionsNow = await db.ActiveConnections.ToListAsync();
        var activeUsers = connectionsNow.Select(conn => new ChatUser { Name = conn.Name, Color = conn.Color }).ToList();
        await Clients.Caller.SendAsync("GetActiveUsers", activeUsers);
    }

    public async Task BroadcastMessage(MessageData messageData)
    {
        bool rateLimited = !CheckRateLimit(Context.ConnectionId);

        if (rateLimited) throw new HubException("Rate limit exceeded");

        var sanitisedMessage = new Message
        {
            Name = SanitiseName(messageData.Name),
            Color = SanitiseColor(messageData.Color),
            Content = SanitiseContent(messageData.Content),
            UnixTime = messageData.UnixTime,
            Id = $"{messageData.Name}." +
                $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
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
        // Once unique names are being used this should be userId = user.name ?? ConnectionId
        if (!_rateLimits.ContainsKey(ConnectionId)) _rateLimits[ConnectionId] = new Queue<DateTime>();

        var queue = _rateLimits[ConnectionId];
        var now = DateTime.UtcNow;
        Console.WriteLine($"Queue: {_rateLimits} This queue: {queue}, timenow: {now}");

        while (queue.Count > 0 && queue.Peek() < now.AddMinutes(-1)) queue.Dequeue();

        if (queue.Count >= _chatSettings.RateLimitPerMinute) return false;

        queue.Enqueue(now);
        return true;

    }


}
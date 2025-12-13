using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Models.Slimechat;

[ApiController]
[Route("api/ServerMessage")]
public class ServerMessageController : ControllerBase
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ServerMessageController> _logger;
    private readonly ChatDb _db;
    private readonly ChatSettings _settings;
    

    public ServerMessageController(IHubContext<ChatHub> hubContext, IOptions<ChatSettings> settings, ILogger<ServerMessageController> logger, ChatDb db)
    {
        _hubContext = hubContext;
        _logger = logger;
        _db = db;
        _settings = settings.Value;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessageAsServer([FromBody] ServerMessageRequest request)
    {
    if (string.IsNullOrEmpty(request.Key)) 
    {
        _logger.LogWarning("No API key received from {Client}", HttpContext.Connection.RemoteIpAddress);
        return Unauthorized("No key provided");
    }
    if (request.Key != _settings.ApiKey) 
    {
        _logger.LogWarning("Invalid API key received from {Client}", HttpContext.Connection.RemoteIpAddress);
        return Unauthorized("Invalid key");
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return BadRequest("Message required");
    }

    _logger.LogInformation("Server broadcast: {Message}", request.Message);

    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var message = new Message
        {
            Id = "System-32" + now,
            UserId = "System-32" + now,
            Name = "🖥️ System",
            Content = request.Message,
            UnixTime = now,
            Type = "user",

        };

    try {
        _db.Add(message);
        _db.SaveChanges();
        await _hubContext.Clients.All.SendAsync("ServerMessage", message);
        } 
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Server message failed to save or send; operation cancelled."
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Server message failed");
            throw new HubException("Failed to save or send server message.");
        }
    
    return Ok();
    }  
}
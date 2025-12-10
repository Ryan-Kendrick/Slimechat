using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Models.Slimechat;

[ApiController]
[Route("api/[controller]")]
public class ServerMessageController : ControllerBase
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ChatSettings _settings;
    private readonly ILogger<ServerMessageController> _logger;

    public ServerMessageController(IHubContext<ChatHub> hubContext, IOptions<ChatSettings> settings, ILogger<ServerMessageController> logger)
    {
        _hubContext = hubContext;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost("message")]
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

       await _hubContext.Clients.All.SendAsync("ServerMessage", new
        {
            content = request.Message,
            type = "system",
            timestamp = DateTime.UnixEpoch
        });

        return Ok(new { success = true, message = "Broadcast sent" });
}
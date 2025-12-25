using Api;
using Api.Authentication;
using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Models.Slimechat;

[ApiController]
[Route("api/ServerMessage")]
[AuthenticationRequired]
public class ServerMessageController(IHubContext<ChatHub> hubContext, IOptions<ApiSettings> settings, ILogger<ServerMessageController> logger, ChatDb db) : ApiControllerBase(db, settings)
{

    [HttpPost]
    public async Task<ActionResult<Message>> SendMessageAsServer([FromBody] ServerMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message required");
        }

        logger.LogInformation("Server broadcast: {Message}", request.Message);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var message = new Message
        {
            Id = "System-32" + now,
            UserId = "System-32" + 1763414400,
            Name = "🖥️ System",
            Content = request.Message,
            UnixTime = now,
            Type = "user",

        };

        try
        {
            Db.Messages.Add(message);
            await Db.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("ServerMessage", message);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Server message failed to save or send; operation cancelled."
            );
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server message failed");
            throw new HubException("Failed to save or send server message.");
        }

        return Ok(message);
    }
}
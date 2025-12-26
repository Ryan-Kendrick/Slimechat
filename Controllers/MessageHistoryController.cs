using Api;
using Api.Authentication;
using Exceptions;
using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

[ApiController]
[Route("api/MessageHistory")]
public class MessageHistoryController(IHubContext<ChatHub> hubContext, IOptions<ApiSettings> settings, ILogger<MessageHistoryController> logger, ChatDb db) : ApiControllerBase(db, settings)
{
    [HttpGet]
    public async Task<ActionResult<List<Message>>> GetMessageHistory([FromBody] MessageHistoryRequest? request, CancellationToken ct)
    {
        var count = request?.Count ?? Settings.GetMessageHistoryMax;
        count = Math.Clamp(count, 1, Settings.GetMessageHistoryMax);

        var messages = await Db.Messages.AsNoTracking().OrderByDescending(m => m.UnixTime).Take(count).ToListAsync(ct);

        return Ok(messages);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<Message>>> GetUserMessageHistory(string userId, [FromBody] MessageHistoryRequest? request, CancellationToken ct)
    {
        var count = request?.Count ?? Settings.GetMessageHistoryMax;
        count = Math.Clamp(count, 1, Settings.GetMessageHistoryMax);

        var messages = await Db.Messages.AsNoTracking().Where(m => m.UserId == userId).OrderByDescending(m => m.UnixTime).Take(count).ToListAsync(ct);

        if (messages.Count() == 0) throw new ResourceNotFoundException($"No messages found for User ID: {userId}");

        return Ok(messages);
    }

    [HttpPut("{messageId}")]
    [AuthenticationRequired]
    public async Task<ActionResult<Message>> PutMessage(string messageId, [FromBody] UpdateMessageContentRequest request, CancellationToken ct)
    {

        var message = await Db.Messages.FindAsync(messageId) ?? throw new ResourceNotFoundException($"Message ID {messageId} not found");

        message.Content = request.NewContent;
        await Db.SaveChangesAsync(ct);


        logger.LogInformation("Message by {User} updated: {Message}", message.Name, message.Content);

        await hubContext.Clients.All.SendAsync("UpdateModifiedMessage", message);

        return Ok(message);
    }

    [HttpDelete("{messageId}")]
    [AuthenticationRequired]
    public async Task<ActionResult<Message>> DeleteMessage(string messageId, CancellationToken ct)
    {
        var message = await Db.Messages.FindAsync([messageId], ct) ?? throw new ResourceNotFoundException($"Message id {messageId} not found");

        Db.Messages.Remove(message);
        await Db.SaveChangesAsync(ct);

        logger.LogInformation("Message by {User} deleted: {Message}", message.Name, message.Content);

        await hubContext.Clients.All.SendAsync("RemoveDeletedMessage", message);

        return NoContent();
    }
}

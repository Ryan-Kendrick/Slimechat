using Api;
using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

[ApiController]
[Route("api/MessageHistory")]
public class MessageHistoryController : ApiControllerBase
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<MessageHistoryController> _logger;

    public MessageHistoryController(IHubContext<ChatHub> hubContext, ILogger<MessageHistoryController> logger, IOptions<ApiSettings> Settings, ChatDb Db) : base(Db, Settings)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<Message>>> GetMessageHistory([FromBody] MessageHistoryRequest? request)
    {
        var count = request?.Count ?? Settings.GetMessageHistoryMax;
        count = Math.Clamp(count, 1, Settings.GetMessageHistoryMax);

        var messages = await Db.Messages.AsNoTracking().OrderByDescending(m => m.UnixTime).Take(count).ToListAsync();

        return Ok(messages);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<Message>>> GetUserMessageHistory(string userId, [FromBody] MessageHistoryRequest? request)
    {
        var count = request?.Count ?? Settings.GetMessageHistoryMax;
        count = Math.Clamp(count, 1, Settings.GetMessageHistoryMax);
        var messages = await Db.Messages.Where(m => m.UserId == userId).AsNoTracking().OrderByDescending(m => m.UnixTime).Take(count).ToListAsync();

        return Ok(messages);
    }

    [HttpPut("{messageId}")]
    public async Task<ActionResult<Message>> PutMessage(string messageId, [FromBody] UpdateMessageContentRequest request, [FromHeader] string key)
    {
        if (key != Settings.ApiKey) return Unauthorized();
        if (request.NewContent == null) return BadRequest();

        var message = await Db.Messages.FindAsync(messageId);
        if (message == null) return NotFound("Message id {messageId} not found");

        message.Content = request.NewContent;
        await Db.SaveChangesAsync();


        _logger.LogInformation("Message by {User} updated: {Message}", message.Name, message.Content);

        await _hubContext.Clients.All.SendAsync("UpdateModifiedMessage", message);

        return Ok(message);
    }

    [HttpDelete("{messageId}")]
    public async Task<ActionResult<Message>> DeleteMessage(string messageId, [FromHeader] string key)
    {
        Console.WriteLine(key);
        if (key != Settings.ApiKey) return Unauthorized();

        var message = await Db.Messages.FindAsync(messageId);
        if (message == null) return NotFound("Message id {messageId} not found");

        Db.Messages.Remove(message);
        await Db.SaveChangesAsync();

        _logger.LogInformation("Message by {User} deleted: {Message}", message.Name, message.Content);

        await _hubContext.Clients.All.SendAsync("RemoveDeletedMessage", message);

        return NoContent();
    }


}

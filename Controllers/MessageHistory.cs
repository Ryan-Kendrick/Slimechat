using Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

[ApiController]
[Route("api/MessageHistory")]
public class MessageHistoryController : ApiControllerBase
{
    private readonly ILogger<MessageHistoryController> _logger;

    public MessageHistoryController(ILogger<MessageHistoryController> logger, IOptions<ApiSettings> Settings, ChatDb Db) : base(Db, Settings)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Message>>> GetMessageHistory([FromBody] MessageHistoryRequest? request)
    {
        var count = request?.Count ?? Settings.GetMessageHistoryMax;
        count = Math.Clamp(count, 1, Settings.GetMessageHistoryMax);
        var messages = await Db.Messages.AsNoTracking().OrderByDescending(m => m.UnixTime).Take(count).ToListAsync();

        return Ok(messages);
    }

    // Add GET MessageHistory/user/{id}
    // Add authenticated PUT MessageHistory/{id}
    // Add authenticated DELETE MessageHistory/{id}
}

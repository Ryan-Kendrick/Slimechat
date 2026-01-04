
using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Models.Slimechat;
using NSubstitute;

namespace Api.Tests;

public class MessageHistoryControllerTests : IDisposable
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly NullLogger<MessageHistoryController> _logger;
    private readonly IOptions<ApiSettings> _settings;
    private readonly IClientProxy _mockChatClient;
    private readonly ChatDb _db;


    public MessageHistoryControllerTests()
    {
        _hubContext = Substitute.For<IHubContext<ChatHub>>();
        _mockChatClient = Substitute.For<IClientProxy>();
        _hubContext.Clients.All.Returns(_mockChatClient);
        _logger = new NullLogger<MessageHistoryController>();

        var options = new DbContextOptionsBuilder<ChatDb>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var chatSettings = Options.Create(new ChatSettings
        {
            NameLengthMax = 50,
            MessageLengthMax = 200
        });

        _db = new ChatDb(options, chatSettings);

        _settings = Options.Create(new ApiSettings
        {
            ApiKey = "testkey",
            GetMessageHistoryMax = 50
        });
    }

    private MessageHistoryController GetController()
    {
        return new MessageHistoryController(_hubContext, _settings, _logger, _db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetMessageHistory_ReturnsMessagesOrderedByDate()
    {
        var controller = GetController();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds;
        _db.Messages.AddRange(
        new Message { Id = $"Slime-001.${now}", UserId = $"Slime-001.${now}", Name = "Slime-001", Content = "Oldest", UnixTime = 10, Type = "user" },
        new Message { Id = $"Slime-002.${now}", UserId = $"Slime-002.${now}", Name = "Slime-002", Content = "Newest", UnixTime = 100, Type = "user" }
    );
        await _db.SaveChangesAsync();

        var result = await controller.GetMessageHistory(null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var messages = Assert.IsType<List<Message>>(okResult.Value);
        Assert.Equal("Newest", messages[0].Content);
    }

    [Fact]
    public async Task PutMessage_UpdatesDbAndNotifiesHub()
    {
        var controller = GetController();

    }
}

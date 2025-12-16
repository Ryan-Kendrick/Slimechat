
using Microsoft.Extensions.Options;

namespace Models.Slimechat;

public class ApiSettings
{
    public string? ApiKey { get; set; }
    public int GetMessageHistoryMax { get; set; }
}
public class ServerMessageRequest
{
    public string Message { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
}

public class MessageHistoryRequest
{
    public int? Count { get; set; }

}

public class UpdateMessageContentRequest
{
    public string NewContent { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

public class DeleteMessageRequest
{
    public string Key { get; set; } = string.Empty;
}

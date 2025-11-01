namespace Models.Slimechat;

public class ChatUser
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; } = string.Empty;
}

public class MessageData
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Color { get; set; } = string.Empty;
    public long unixTime { get; set; } = 0;

}

public class ChatSettings
{
    public int MessageLengthMax { get; set; } = 0;
    public int NameLengthMax { get; set; } = 0;
    public int MessageHistoryMax { get; set; } = 0;
    public int RateLimitPerMinute { get; set; } = 0;
}
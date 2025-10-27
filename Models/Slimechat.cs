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
    public long unixTime { get; set; } = long.MinValue;

}
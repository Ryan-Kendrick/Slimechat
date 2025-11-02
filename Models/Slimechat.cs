using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Models.Slimechat;

public class ChatUser
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; } = string.Empty;
}

public class MessageData
{
    public string? Name { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Color { get; set; }
    public long? UnixTime { get; set; }
    public string Type { get; set; } = "user"; // system or user
}

public class Message : MessageData
{
    public string Id { get; set; } = string.Empty;
}

public class ChatSettings
{
    public int MessageLengthMax { get; set; } = 0;
    public int NameLengthMax { get; set; } = 0;
    public int MessageHistoryMax { get; set; } = 0;
    public int RateLimitPerMinute { get; set; } = 0;
}

public class ChatDb : DbContext
{
    private readonly ChatSettings _chatSettings;
    public ChatDb(DbContextOptions options, IOptions<ChatSettings> chatSettings)
        : base(options)
    {
        _chatSettings = chatSettings.Value;
    }
    public DbSet<Message> Messages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(_chatSettings.NameLengthMax + 13)
                .ValueGeneratedNever();
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(_chatSettings.MessageLengthMax);
            entity.Property(e => e.Name)
                .HasMaxLength(_chatSettings.NameLengthMax);
            entity.Property(e => e.Color)
               .HasMaxLength(7);
            entity.Property(e => e.UnixTime);
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(6);
        });
    }
}
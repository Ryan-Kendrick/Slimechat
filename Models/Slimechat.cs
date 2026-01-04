using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Models.Slimechat;


public class ChatUser
{
    public required string Id { get; set; } = string.Empty;
    public required string Name { get; set; } = string.Empty;
    public required string Color { get; set; } = string.Empty;
}
public class ActiveConnection : ChatUser
{
    public string ConnectionId { get; set; } = string.Empty;
}

public class MessageData
{
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Content { get; set; }
    public string? Color { get; set; } = "#000";
    public required long UnixTime { get; set; }
    public required string Type { get; set; } = "user"; // system or user
}

public class Message : MessageData
{
    public required string Id { get; set; }
}

public class ChatSettings
{
    public int MessageLengthMax { get; set; } = 0;
    public int OnJoinMessageHistoryMax { get; set; } = 0;
    public int MessageHistoryMax { get; set; } = 0;
    public int MessageCleanupServiceInterval { get; set; } = int.MaxValue;
    public int NameLengthMax { get; set; } = 0;
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
    public DbSet<ActiveConnection> ActiveConnections { get; set; } = null!;

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
            entity.Property(e => e.UserId)
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

        modelBuilder.Entity<ActiveConnection>(entity =>
      {
          entity.HasKey(e => e.ConnectionId);
          entity.Property(e => e.ConnectionId)
              .IsRequired()
              .ValueGeneratedNever();
          entity.Property(e => e.Id)
              .IsRequired()
              .ValueGeneratedNever();
          entity.Property(e => e.Name)
              .IsRequired()
              .HasMaxLength(_chatSettings.NameLengthMax);
          entity.Property(e => e.Color)
              .IsRequired()
              .HasMaxLength(7);
      });
    }
}

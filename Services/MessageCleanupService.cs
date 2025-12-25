using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

public class MessageCleanupService(IOptions<ChatSettings> settings, ILogger<MessageCleanupService> logger, IDbContextFactory<ChatDb> dbContextFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        int cleanupInterval = settings.Value.MessageCleanupServiceInterval;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(cleanupInterval));

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await CleanupMessagesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during message cleanup");
            }
        }
    }

    private async Task CleanupMessagesAsync(CancellationToken ct)
    {
        using var db = await dbContextFactory.CreateDbContextAsync(ct);

        int messageLimit = settings.Value.MessageHistoryMax;

        var cutoffTime = await db.Messages
                   .OrderByDescending(m => m.UnixTime)
                   .Skip(messageLimit)
                   .Select(m => m.UnixTime)
                   .FirstOrDefaultAsync(ct);


        logger.LogInformation("Message cleanup service running, deleting messages from before time {Time}", cutoffTime);
        if (cutoffTime > 0)
        {
            var deletedRows = await db.Messages.Where(m => m.UnixTime <= cutoffTime).ExecuteDeleteAsync(ct);

            if (deletedRows > 0)
            {
                logger.LogInformation("Maintenance: Purged {Count} old messages.", deletedRows);
            }
        }
    }
}
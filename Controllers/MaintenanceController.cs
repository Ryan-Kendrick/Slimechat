using Api.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Api.Controllers;

[ApiController]
[Route("api/System")]
[AuthenticationRequired]
public class SystemController(ChatDb db, IOptions<ApiSettings> settings, ILogger<SystemController> logger) : ApiControllerBase(db, settings)
{
    [HttpPost("vacuum")]
    public async Task<IActionResult> Vacuum()
    {
        try
        {
            logger.LogInformation("Manual VACUUM requested.");
            await Db.Database.ExecuteSqlRawAsync("VACUUM;");
            return Ok("Database vacuumed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during manual vacuum");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("DropDb")]
    public IActionResult DeleteDb()
    {
        // This is a terrible idea, but it is fun
        try
        {
            var dbPath = Db.Database.GetDbConnection().DataSource;
            logger.LogWarning("Manual Database deletion requested for path: {Path}", dbPath);

            // Close connections before deleting
            Db.Database.GetDbConnection().Close();
            SqliteConnection.ClearAllPools();

            var filesToDelete = new[]
          {
            dbPath,
            dbPath + "-shm",
            dbPath + "-wal",
        };

            var deleted = false;

            foreach (var file in filesToDelete)
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                    deleted = true;
                    logger.LogInformation("Deleted: {File}", file);
                }
            }

            return deleted
                ? Ok("Database and journal files deleted.")
                : NotFound("No database files found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database deletion");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("CreateDb")]
    public IActionResult CreateDb()
    {
        try
        {
            var dbPath = Db.Database.GetDbConnection().DataSource;
            logger.LogWarning("Manual Database creation requested for path: {Path}", dbPath);

            Db.Database.Migrate();
            Db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;"); // https://github.com/dotnet/efcore/issues/36513#issuecomment-3167179043

            return Ok("Database created");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database deletion");
            return StatusCode(500, ex.Message);
        }
    }

}

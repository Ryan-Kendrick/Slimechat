using Api.Authentication;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("dropdb")]
    public IActionResult DeleteDb()
    {
        try
        {
            var dbPath = Db.Database.GetDbConnection().DataSource;
            logger.LogWarning("Manual Database Deletion requested for path: {Path}", dbPath);

            // Close connections before deleting
            Db.Database.GetDbConnection().Close();

            if (System.IO.File.Exists(dbPath))
            {
                System.IO.File.Delete(dbPath);
                return Ok("Database deleted.");
            }
            return NotFound("Database file not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database deletion");
            return StatusCode(500, ex.Message);
        }
    }
}

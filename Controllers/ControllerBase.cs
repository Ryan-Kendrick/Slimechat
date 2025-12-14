using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Api;

[ApiExplorerSettings(IgnoreApi = true)]
public abstract class ApiControllerBase : ControllerBase
{
    protected ChatDb Db { get; }
    protected ApiSettings Settings { get; }

    protected ApiControllerBase(ChatDb db, IOptions<ApiSettings> settings)
    {
        Db = db;
        Settings = settings.Value;
    }
}
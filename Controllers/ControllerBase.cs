using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Api;

[ApiExplorerSettings(IgnoreApi = true)]
public abstract class ApiControllerBase : ControllerBase
{
    protected ChatDb Db { get; }
    protected ChatSettings Settings { get; }

    protected ApiControllerBase(ChatDb db, IOptions<ChatSettings> settings) {
        Db = db;
        Settings = settings.Value;
    }
}
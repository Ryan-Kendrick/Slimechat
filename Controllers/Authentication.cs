using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Api.Authentication;

public class AuthenticationRequiredAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var settings = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiSettings>>().Value;
        var keyInHeader = context.HttpContext.Request.Headers.TryGetValue("key", out var providedKey);

        if (!keyInHeader || providedKey != settings.ApiKey)
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid or missing API key"
            });
        }
    }
}
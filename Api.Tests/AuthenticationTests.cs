using Api.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Models.Slimechat;

namespace Api.Tests.Authentication;

public class AuthenticationRequiredAttributeTests
{
    private const string ValidKey = "testkey";

    private static AuthorizationFilterContext CreateContext(string? apiKey)
    {
        var services = new ServiceCollection()
            .AddSingleton(Options.Create(new ApiSettings { ApiKey = ValidKey }))
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };

        if (apiKey != null) httpContext.Request.Headers["key"] = apiKey;

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>()
        );
    }

    [Fact]
    public void OnAuthorization_ValidKey_AllowsAccess()
    {
        var context = CreateContext(ValidKey);

        new AuthenticationRequiredAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("wrongkey")]
    [InlineData(null)]
    [InlineData("")]
    public void OnAuthorization_InvalidKey_ReturnsUnauthorized(string? apiKey)
    {
        var context = CreateContext(apiKey);

        new AuthenticationRequiredAttribute().OnAuthorization(context);

        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Equal("Invalid or missing API key", problem.Detail);
    }

}
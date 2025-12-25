using Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exception: {Message}", exception.Message);

        var (statusCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    public static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),

            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "The requested key was not present."),
            OperationCanceledException => (StatusCodes.Status408RequestTimeout, "Timeout", "Request timed out."),

            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
    }
}
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Shared.Exceptions;

public class InternalConfigurationExceptionHandler(ILogger<InternalConfigurationExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        
        if (exception is not InternalConfigurationException)
            return false;

        
        logger.LogError(
            exception,
            "An unexpected error occurred while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);
        logger.LogError(
            exception,
            $"Internal {exception.Message}");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;


        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Title = "An unexpected error occurred, we're working to solve this porblem.", },
            cancellationToken);

        return true;
    }
}
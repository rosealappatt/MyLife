using Microsoft.AspNetCore.Diagnostics;

namespace MyLife.API.Middleware;

/// <summary>
/// Centralized exception handler for unhandled exceptions
/// that occur while processing an HTTP request.
///
/// Instead of every controller/service having its own
/// try-catch block, ASP.NET Core sends unhandled exceptions
/// to this class.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// This method is called by ASP.NET Core whenever an
    /// unhandled exception reaches the exception-handling middleware.
    /// </summary>
    /// 
    /// <param name="httpContext">
    /// Represents the current HTTP request and response.
    /// </param>
    ///
    /// <param name="exception">
    /// The exception that was not handled elsewhere.
    /// </param>
    ///
    /// <param name="cancellationToken">
    /// Allows the operation to be cancelled if the client
    /// disconnects or the request is cancelled.
    /// </param>
    ///
    /// <returns>
    /// true = the exception has been handled, do not pass it to another handler.
    /// false = another exception handler should get a chance.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred.");

        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,

            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,

            KeyNotFoundException => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status500InternalServerError
        };

        // Set the HTTP status code that will be returned to the client.
        httpContext.Response.StatusCode = statusCode;

        /*
         * Return a simple JSON response to the client.
         * Notice that we are NOT returning exception message directly to the client.
         * This is intentional because exception messages can accidentally expose internal implementation details.
         * This is an important PRODUCTION APPLICATION DESIGN PRINCIPLE.
         */
        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                title = "An error occurred while processing your request.",
                statusCode,
                traceId = httpContext.TraceIdentifier // TraceIdentifier allows us to correlate the response received by the client with the corresponding log entry.
            },
            cancellationToken);

        return true;
    }
}
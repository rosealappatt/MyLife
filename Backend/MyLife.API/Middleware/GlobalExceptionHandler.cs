using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MyLife.API.Middleware;

/// <summary>
/// Centralized handler for unhandled exceptions.
///
/// This class is responsible for:
/// 1. Logging the exception.
/// 2. Determining the appropriate HTTP status code.
/// 3. Creating a standardized ProblemDetails response.
/// 4. Returning the response to the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    // ASP.NET Core provides ILogger through dependency injection.
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Constructor.
    /// ILogger is automatically supplied by ASP.NET Core's dependency injection container.
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

        // Create a standardized ProblemDetails response.
        // ProblemDetails gives our API a consistent structure for communicating HTTP errors.
        var problemDetails = new ProblemDetails
        {
            // A human-readable summary of the problem.
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "The request is invalid.",

                StatusCodes.Status401Unauthorized => "Authentication is required.",

                StatusCodes.Status404NotFound => "The requested resource was not found.",

                _ => "An error occurred while processing your request."
            },

            // HTTP status code associated with the problem.
            Status = statusCode,

            // The API endpoint where the problem occurred.
            Instance = httpContext.Request.Path
        };

        // Add our trace ID as an extension.
        //
        // This is useful because the client can provide this ID
        // to us when reporting an error, allowing us to find the
        // corresponding server-side log entry.
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        // Tell the client that we are returning an HTTP ProblemDetails response.
        httpContext.Response.ContentType = "application/problem+json";

        // Set the HTTP status code.
        httpContext.Response.StatusCode = statusCode;

        // Serialize the ProblemDetails object as JSON.
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
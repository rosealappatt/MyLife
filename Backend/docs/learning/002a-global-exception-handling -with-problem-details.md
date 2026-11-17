# Global Exception Handling and ProblemDetails

## where? 
builder.Services.AddProblemDetails(); in program.cs

## why?
Problem Details is a STANDARDIZED ERROR RESPONSE for HTTP API errors, based on RFC 9457. 

It gives errors a predictable structure such as:
{
  "type": "...",
  "title": "Bad Request",
  "status": 400,
  "detail": "...",
  "instance": "..."
}

The idea is that different errors from your API can have a consistent format.

That becomes especially useful once MyLife has:
- validation errors
- not-found errors
- authorization errors
- conflict errors
- unexpected 500 errors
because your Angular frontend can expect a consistent error structure.


builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); -> "Here is the code that should handle unhandled exceptions." (WHO handles the exception?)
app.UseExceptionHandler(); -> "Put exception handling into the HTTP request pipeline." (WHEN/WHERE should exception handling participate in the pipeline?)
builder.Services.AddProblemDetails(); -> "Register the infrastructure for producing standardized Problem Details error responses." (HOW should standardized HTTP errors be represented?)

That gives you a much better interview story:
"I use ASP.NET Core's IExceptionHandler for centralized exception handling and Problem Details for a consistent HTTP error contract. 
The handler logs the actual exception server-side and returns a sanitized Problem Details response containing a trace ID."

That's considerably stronger than simply saying:
"I catch exceptions globally and return JSON."


## 1. What problem are we solving?

An API can encounter unexpected errors while processing a request.

Examples include:

- Database failures
- Invalid operations
- Missing resources
- Authorization failures
- Unexpected programming errors
- External service failures

Without centralized exception handling, every controller or service may start adding its own `try-catch` blocks.

This creates duplicated code and inconsistent error responses.

MyLife uses ASP.NET Core's global exception handling mechanism to centralize the handling of unhandled exceptions.

---

## 2. The problem with handling exceptions in every controller

A controller could handle exceptions like this:

```csharp
try
{
    // Perform operation
}
catch (Exception exception)
{
    // Log exception
    // Return error response
}
```

Doing this in every controller creates several problems:

- duplicated code
- inconsistent error responses
- inconsistent logging
- difficult maintenance
- increased chance of forgetting to handle an exception

Instead, MyLife uses a centralized exception handler.

---

## 3. MyLife's exception-handling flow

```text
HTTP Request
      |
      v
ASP.NET Core Pipeline
      |
      v
Controller
      |
      v
Application Layer
      |
      v
Infrastructure Layer
      |
      v
Exception occurs
      |
      v
GlobalExceptionHandler
      |
      +----> Log exception
      |
      +----> Determine HTTP status code
      |
      +----> Create ProblemDetails
      |
      +----> Return standardized error response
```

This keeps exception-handling concerns out of normal controller logic.

---

## 4. `IExceptionHandler`

MyLife's handler implements:

```csharp
IExceptionHandler
```

The class is:

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
```

The main method is:

```csharp
TryHandleAsync(...)
```

ASP.NET Core calls this method when an unhandled exception reaches the exception-handling middleware.

---

## 5. Why is `GlobalExceptionHandler` in MyLife.API?

The handler uses HTTP-specific concepts such as:

```csharp
HttpContext
StatusCodes
IExceptionHandler
```

These belong to the API layer.

Therefore:

```text
MyLife.API
└── Middleware
    └── GlobalExceptionHandler.cs
```

The Application and Domain layers should not depend on HTTP-specific infrastructure.

This preserves the separation between the layers.

---

## 6. Dependency Injection

The handler receives:

```csharp
ILogger<GlobalExceptionHandler>
```

through its constructor:

```csharp
public GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
{
    _logger = logger;
}
```

ASP.NET Core's dependency injection container creates the handler and supplies the logger.

The class therefore does not need to create its own logger.

---

## 7. Logging the exception

The handler uses:

```csharp
_logger.LogError(
    exception,
    "Unhandled exception occurred.");
```

The complete exception is passed to the logger.

This allows the logs to contain useful debugging information such as:

- exception type
- exception message
- stack trace
- inner exception

The complete exception is not exposed directly to the API client.

---

## 8. HTTP status code mapping

The handler currently maps standard exception types:

```csharp
var statusCode = exception switch
{
    ArgumentException =>
        StatusCodes.Status400BadRequest,

    UnauthorizedAccessException =>
        StatusCodes.Status401Unauthorized,

    KeyNotFoundException =>
        StatusCodes.Status404NotFound,

    _ =>
        StatusCodes.Status500InternalServerError
};
```

Current mapping:

| Exception | HTTP status |
|---|---:|
| `ArgumentException` | 400 Bad Request |
| `UnauthorizedAccessException` | 401 Unauthorized |
| `KeyNotFoundException` | 404 Not Found |
| Unknown exception | 500 Internal Server Error |

The `_` case ensures unexpected exceptions are treated as internal server errors.

---

# 9. What is ProblemDetails?

ProblemDetails is a standardized format for representing HTTP API errors.

Instead of every API creating its own error structure, the API can return a consistent representation.

For example:

```json
{
  "title": "An error occurred while processing your request.",
  "status": 500,
  "instance": "/api/test/exception",
  "traceId": "0H..."
}
```

The standard fields used by ProblemDetails include concepts such as:

- `type`
- `title`
- `status`
- `detail`
- `instance`

Applications can also add extension properties.

MyLife currently adds:

```text
traceId
```

as an extension.

---

## 10. Why use ProblemDetails instead of anonymous JSON?

An anonymous response such as:

```csharp
new
{
    title = "...",
    statusCode = 500,
    traceId = "..."
}
```

is simply a custom JSON structure.

It is not a standardized Problem Details object.

Using:

```csharp
new ProblemDetails
{
    Title = "...",
    Status = 500,
    Instance = "..."
}
```

gives MyLife a standard error contract.

This is particularly useful as the API grows because Angular or another client can handle errors consistently.

---

## 11. `AddProblemDetails()`

In `Program.cs`, MyLife registers:

```csharp
builder.Services.AddProblemDetails();
```

This registers ASP.NET Core's Problem Details infrastructure with dependency injection.

It is important to understand that `AddProblemDetails()` and the custom exception handler have different responsibilities.

### `AddExceptionHandler`

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

Registers the code that handles the exception.

### `UseExceptionHandler`

```csharp
app.UseExceptionHandler();
```

Activates exception handling in the HTTP request pipeline.

### `AddProblemDetails`

```csharp
builder.Services.AddProblemDetails();
```

Registers the Problem Details infrastructure used for standardized API error responses.

Therefore:

```text
AddExceptionHandler
        |
        v
Register custom exception handler

UseExceptionHandler
        |
        v
Activate exception handling middleware

AddProblemDetails
        |
        v
Register standardized Problem Details support
```

---

## 12. Creating the ProblemDetails response

The handler creates:

```csharp
var problemDetails = new ProblemDetails
{
    Title = "An error occurred while processing your request.",
    Status = statusCode,
    Instance = httpContext.Request.Path
};
```

The trace ID is then added as an extension:

```csharp
problemDetails.Extensions["traceId"] =
    httpContext.TraceIdentifier;
```

This allows MyLife to provide useful diagnostic information without exposing the actual exception.

---

## 13. Why don't we return `exception.Message`?

We intentionally do not expose:

```csharp
exception.Message
```

directly to the client.

An exception message may expose internal information such as:

- database details
- internal class names
- file paths
- SQL information
- implementation details

Instead, the client receives a safe error response.

The detailed exception remains in the server logs.

---

## 14. Why do we use a trace ID?

The response contains:

```csharp
problemDetails.Extensions["traceId"] =
    httpContext.TraceIdentifier;
```

Suppose the client receives:

```text
traceId = ABC123
```

The client can provide that value when reporting an error.

A developer can then search application logs for:

```text
ABC123
```

and locate the corresponding request and exception.

This creates a basic correlation mechanism between the client and server.

---

## 15. Content type

The handler sets:

```csharp
httpContext.Response.ContentType =
    "application/problem+json";
```

This tells the client that the response is a JSON representation of an HTTP Problem Details document.

This is preferable to simply returning:

```text
application/json
```

for an API error that follows the Problem Details format.

---

## 16. Temporary test endpoint

A temporary controller was created to verify the implementation:

```text
MyLife.API
└── Controllers
    └── ExceptionTestController.cs
```

The endpoint deliberately throws:

```csharp
throw new Exception("This is a test exception.");
```

The endpoint is:

```text
GET /api/test/exception
```

There is intentionally no `try-catch` around the exception.

This allows the exception to travel through the ASP.NET Core pipeline.

Expected flow:

```text
GET /api/test/exception
          |
          v
ExceptionTestController
          |
          v
throw Exception
          |
          v
UseExceptionHandler()
          |
          v
GlobalExceptionHandler
          |
          +----> Log exception
          |
          +----> Create ProblemDetails
          |
          v
HTTP 500 response
```

The endpoint exists only for infrastructure testing.

After verification, it should be deleted.

---

## 17. Why is the test controller temporary?

The controller does not represent actual MyLife functionality.

It exists only to verify that:

- the exception handler is registered
- the middleware is active
- exceptions reach the handler
- exceptions are logged
- a 500 response is generated
- ProblemDetails is returned

After successful testing, the controller should be removed.

---

## 18. Current implementation

```text
MyLife.API
│
├── Middleware
│   └── GlobalExceptionHandler.cs
│
├── Controllers
│   └── ExceptionTestController.cs   ← temporary
│
└── Program.cs
```

Relevant registrations:

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();
```

Relevant middleware:

```csharp
app.UseExceptionHandler();
```

---

## 19. Future improvements

The current implementation uses standard .NET exceptions.

As the MyLife Application layer grows, we will introduce application-specific exceptions such as:

```text
NotFoundException
ValidationException
ConflictException
UnauthorizedException
```

The global handler can then map these more explicitly:

```text
NotFoundException
        ↓
404 Not Found

ValidationException
        ↓
400 Bad Request

ConflictException
        ↓
409 Conflict

UnauthorizedException
        ↓
401 Unauthorized

Unexpected exception
        ↓
500 Internal Server Error
```

At that point, the ProblemDetails response can also contain more useful information for validation and business errors.

---

## 20. Key concepts learned

### Global exception handling

Centralizes handling of unhandled exceptions.

### `IExceptionHandler`

ASP.NET Core interface used to implement custom centralized exception handling.

### `AddExceptionHandler<T>()`

Registers the custom exception handler.

### `UseExceptionHandler()`

Activates exception handling in the HTTP request pipeline.

### `ProblemDetails`

Standardized representation of HTTP API errors.

### `AddProblemDetails()`

Registers Problem Details support.

### `ILogger`

ASP.NET Core logging abstraction used to record application events and exceptions.

### Trace ID

Identifier used to correlate an API response with server-side logs.

### `application/problem+json`

Content type used for Problem Details JSON responses.

---

## 21. Interview explanation

If asked:

> How do you handle exceptions globally in ASP.NET Core?

A good answer is:

> "I use ASP.NET Core's `IExceptionHandler` to centralize handling of unhandled exceptions. 
I register the handler using `AddExceptionHandler` and activate it with `UseExceptionHandler`. 
The handler logs the original exception using `ILogger`, 
maps known exceptions to appropriate HTTP status codes, 
and returns a sanitized Problem Details response instead of exposing internal exception details. 
I also include a trace ID so the client error can be correlated with server-side logs."

If asked:

> Why use ProblemDetails?

A good answer is:

> "ProblemDetails gives the API a standardized error response contract 
instead of having every endpoint return its own JSON structure. 
This makes error handling more consistent for API consumers and 
provides a standard place for information such as the 
HTTP status, title, detail, instance, and application-specific extensions such as a trace ID."
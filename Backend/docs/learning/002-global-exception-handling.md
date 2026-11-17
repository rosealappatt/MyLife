Goal: 
What happens when something goes wrong in an API request, and how can we make sure every unexpected exception is handled consistently?

We’ll make the changes in this order:
1. GlobalExceptionHandler.cs — create the actual exception handler
2. Program.cs — register and activate it
3. Build the solution
4. Create a temporary endpoint that deliberately throws an exception
5. Test the response in Scalar
6. Remove the temporary endpoint
7. Add learning/decision documentation

# Global Exception Handling

## 1. What problem are we solving?

An API can encounter unexpected errors while processing a request.

For example:

- Database connection failure
- Null reference
- Invalid operation
- Unexpected business logic failure
- External API failure
- Programming bug

Without centralized exception handling, every controller or service might start adding its own `try-catch` blocks.

That can lead to duplicated code and inconsistent error responses.

MyLife uses ASP.NET Core's global exception handling mechanism so that unhandled exceptions can be processed in one centralized location.

---

## 2. The problem with handling exceptions in every controller

A controller could technically do this:

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

However, repeating this pattern across many controllers is undesirable.

For example:

```text
Controller A
    └── try/catch

Controller B
    └── try/catch

Controller C
    └── try/catch

Controller D
    └── try/catch
```

This creates:

- duplicated code
- inconsistent error responses
- inconsistent logging
- more code to maintain
- a higher chance of developers forgetting to handle an exception

Instead, MyLife centralizes unexpected exception handling.

---

## 3. MyLife's approach

The current flow is:

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
      +----> Return safe error response
```

The important point is that the controller does not need to know how an unexpected exception should be logged or formatted.

---

## 4. `IExceptionHandler`

MyLife's `GlobalExceptionHandler` implements:

```csharp
IExceptionHandler
```

ASP.NET Core provides this interface for centralized exception handling.

Our class is:

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

The handler works directly with HTTP concepts such as:

```csharp
HttpContext
StatusCodes
IExceptionHandler
```

These belong to the ASP.NET Core/API layer.

Therefore, the handler is located in:

```text
MyLife.API
└── Middleware
    └── GlobalExceptionHandler.cs
```

The Application and Domain layers should not depend on HTTP-specific concepts.

This keeps the architecture separated.

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

This demonstrates one of the reasons dependency injection is useful:

The class does not have to manually create its logger.

---

## 7. Logging the exception

The handler uses:

```csharp
_logger.LogError(
    exception,
    "Unhandled exception occurred.");
```

The complete exception is passed to the logger.

This is important because the exception contains information such as:

- Exception type
- Exception message
- Stack trace
- Inner exception

The API response does not expose all of this information to the client.

---

## 8. HTTP status code mapping

The handler currently maps some standard exception types:

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

The current mapping is:

| Exception | HTTP status |
|---|---:|
| `ArgumentException` | 400 Bad Request |
| `UnauthorizedAccessException` | 401 Unauthorized |
| `KeyNotFoundException` | 404 Not Found |
| Unknown exception | 500 Internal Server Error |

The `_` case is important.

It means that exceptions we have not explicitly mapped are treated as internal server errors.

---

## 9. Why shouldn't we return `exception.Message`?

We intentionally do not return:

```csharp
exception.Message
```

directly to the client.

An exception message may contain sensitive internal information such as:

- Database details
- Connection information
- Internal class names
- File paths
- SQL information
- Implementation details

Instead, the client receives a safe response such as:

```json
{
    "title": "An error occurred while processing your request.",
    "statusCode": 500,
    "traceId": "..."
}
```

The detailed exception remains available in the server logs.

---

## 10. Why do we return a trace ID?

The response contains:

```csharp
traceId = httpContext.TraceIdentifier
```

The trace ID allows us to correlate the client response with server-side logs.

For example:

```text
Client receives:

traceId = ABC123
```

A developer can then search the application logs for:

```text
ABC123
```

and find the corresponding exception.

This is especially useful when diagnosing production problems.

---

## 11. Registering the exception handler

In `Program.cs` we register:

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

This tells ASP.NET Core's dependency injection system about our handler.

We also register:

```csharp
builder.Services.AddProblemDetails();
```

This enables ProblemDetails support for standardized HTTP API error responses.

---

## 12. Activating the middleware

Registration alone is not enough.

We also add:

```csharp
app.UseExceptionHandler();
```

to the HTTP request pipeline.

The distinction is:

```text
AddExceptionHandler<T>()
        |
        v
Register the handler


UseExceptionHandler()
        |
        v
Use exception handling
in the HTTP request pipeline
```

Both are required for our current implementation.

---

## 13. Temporary test endpoint

To verify the implementation, we created a temporary controller:

```text
Controllers/
└── ExceptionTestController.cs
```

The endpoint deliberately throws:

```csharp
throw new Exception("This is a test exception.");
```

There is intentionally no `try-catch` around it.

This allows the exception to travel through the ASP.NET Core pipeline and reach `GlobalExceptionHandler`.

The test endpoint is:

```text
GET /api/test/exception
```

Expected result:

```text
HTTP 500 Internal Server Error
```

with a safe JSON response containing:

- title
- status code
- trace ID

The exception itself should also be recorded in the application logs.

---

## 14. Why is the test controller temporary?

The controller exists only to verify infrastructure.

It is not part of the actual MyLife business functionality.

After confirming that global exception handling works, the controller should be deleted.

This prevents test-only endpoints from accidentally becoming part of the production API.

---

## 15. Future improvements

The current implementation uses standard .NET exceptions.

As the MyLife Application layer grows, we will introduce application-specific exceptions such as:

```text
NotFoundException
ValidationException
ConflictException
UnauthorizedException
```

Then the global exception handler can map them more explicitly:

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

This will make the error-handling design better aligned with the application's business behavior.

---

## 16. Key concepts learned

### Global exception handling

A centralized mechanism for handling exceptions that were not handled elsewhere.

### `IExceptionHandler`

ASP.NET Core interface used to implement custom centralized exception handling.

### `UseExceptionHandler()`

Adds exception handling to the ASP.NET Core request pipeline.

### `AddExceptionHandler<T>()`

Registers the custom exception handler with dependency injection.

### `ILogger`

ASP.NET Core logging abstraction used to record application events and exceptions.

### HTTP status codes

Used to communicate the outcome of an HTTP request to the client.

### Trace ID

An identifier used to correlate an API response with server-side logs.

### ProblemDetails

A standardized approach for representing HTTP API errors.

---

## 17. Interview explanation

If asked:

> "How do you handle exceptions globally in ASP.NET Core?"

A good answer is:

> "I use ASP.NET Core's global exception handling mechanism with `IExceptionHandler`. I register the custom handler using `AddExceptionHandler` and activate the middleware using `UseExceptionHandler`. The handler logs the original exception using `ILogger`, maps known application or framework exceptions to appropriate HTTP status codes, and returns a safe error response to the client rather than exposing internal exception details. I also include a trace ID so the client error can be correlated with server-side logs."

---

## 18. Current MyLife implementation

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

The temporary controller will be removed after verification.
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

This
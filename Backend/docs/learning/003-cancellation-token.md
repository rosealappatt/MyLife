# CancellationToken in ASP.NET Core

##
Cancellation is not necessarily an application error.
It's often simply: "The client no longer wants this operation."
That's why cancellation needs to be handled deliberately rather than blindly turning every exception into a 500.
We'll improve our global exception handling later to account for cancellation appropriately.


This is a good next step because it is a small feature that teaches an important real-world backend concept before we introduce JWT, CQRS, repositories, etc.
We will not build the full cancellation scenario yet. First we'll understand and implement the basic request-cancellation flow.

And importantly, the token is not something we manually create for every request. -> (NO var cancellationToken = new CancellationToken();)
ASP.NET Core creates the request cancellation token and can cancel it when the HTTP request is aborted.


## 1. What problem are we solving?

Some API operations can take time to complete.

Examples include:

- large database queries
- report generation
- file processing
- external API calls
- long-running calculations
- large exports

The client may cancel the HTTP request before the operation completes.

If the server continues processing unnecessarily, it can waste:

- CPU
- memory
- database connections
- network resources
- application capacity

`CancellationToken` provides a cooperative mechanism for stopping work that is no longer required.

---

## 2. What is a CancellationToken?

A `CancellationToken` is a signal that tells an operation:

> "Cancellation has been requested."

It does not forcibly kill a thread.

Instead, the code performing the operation must observe the token and stop appropriately.

This is called **cooperative cancellation**.

---

## 3. Request cancellation in ASP.NET Core

ASP.NET Core can provide a `CancellationToken` representing the lifetime of the current HTTP request.

For example:

```csharp
[HttpGet]
public async Task<IActionResult> GetSomething(
    CancellationToken cancellationToken)
{
    // Use the token here.
}
```

The token is provided by ASP.NET Core.

We don't need to create it manually.

---

## 4. Passing the token to asynchronous operations

A cancellation token is useful only when the operation being performed supports cancellation.

For example:

```csharp
await Task.Delay(
    TimeSpan.FromSeconds(10),
    cancellationToken);
```

The token is passed to `Task.Delay`.

If cancellation is requested, the delay can terminate early.

---

## 5. CancellationToken flow

In a layered application, the token should generally be propagated through the layers.

```text
HTTP Request
      |
      v
Controller
      |
      | CancellationToken
      v
Application
      |
      | CancellationToken
      v
Infrastructure
      |
      | CancellationToken
      v
EF Core / HTTP client / other async operation
```

For example:

```csharp
public async Task<IActionResult> GetSomething(
    CancellationToken cancellationToken)
{
    var result =
        await service.GetSomethingAsync(cancellationToken);

    return Ok(result);
}
```

Application layer:

```csharp
public async Task<Something> GetSomethingAsync(
    CancellationToken cancellationToken)
{
    return await repository.GetSomethingAsync(
        cancellationToken);
}
```

Infrastructure:

```csharp
await dbContext.Somethings
    .ToListAsync(cancellationToken);
```

The same cancellation signal is propagated down the call chain.

---

## 6. Cooperative cancellation

CancellationToken does not forcibly terminate arbitrary code.

Consider:

```csharp
while (true)
{
    // Do work
}
```

Simply passing a CancellationToken somewhere else does not automatically stop this loop.

The code needs to observe cancellation.

For example:

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    // Do work
}
```

Or an asynchronous operation that accepts the token can observe it internally.

---

## 7. `ThrowIfCancellationRequested`

Code can explicitly check for cancellation:

```csharp
cancellationToken.ThrowIfCancellationRequested();
```

If cancellation has been requested, this throws:

```text
OperationCanceledException
```

This allows the operation to exit without continuing unnecessary work.

---

## 8. Cancellation versus failure

Cancellation is conceptually different from an application failure.

For example:

```text
Database connection failed
        ↓
Application failure

User cancelled request
        ↓
Operation no longer required
```

A cancelled request should therefore not automatically be treated as an unexpected server failure.

This distinction becomes important when designing global exception handling and logging.

---

## 9. Why CancellationToken is important for database operations

EF Core provides many asynchronous APIs that accept a cancellation token.

For example:

```csharp
await dbContext.MedicalTests
    .ToListAsync(cancellationToken);
```

If the request is cancelled while the database operation is running, EF Core can propagate the cancellation to the underlying operation where supported.

This is preferable to:

```csharp
await dbContext.MedicalTests
    .ToListAsync();
```

for request-scoped operations where cancellation matters.

---

## 10. Why CancellationToken is important for HTTP calls

When MyLife eventually calls an external API, the token can also be propagated:

```csharp
await httpClient.GetAsync(
    requestUri,
    cancellationToken);
```

If the original HTTP request is cancelled, the downstream operation can also be cancelled.

This prevents unnecessary work from continuing after the user has left the request.

---

## 11. Why CancellationToken is important for large operations

Consider an expense export:

```text
User
  |
  | GET /api/expenses/export
  v
Controller
  |
  v
Application
  |
  v
Database query
  |
  v
Generate Excel
  |
  v
Return file
```

If the user cancels halfway through:

Without cancellation:

```text
Client cancels
      |
      X
      |
      v
Server continues
      |
      v
Database continues
      |
      v
Excel continues generating
```

With cancellation:

```text
Client cancels
      |
      v
CancellationToken
      |
      v
Application
      |
      v
Database / processing
      |
      v
Operation stops
```

This can reduce unnecessary resource consumption.

---

## 12. Common mistake

A common mistake is accepting a token in the controller but not passing it downstream.

For example:

```csharp
public async Task<IActionResult> GetSomething(
    CancellationToken cancellationToken)
{
    var result =
        await service.GetSomethingAsync();

    return Ok(result);
}
```

The token exists but is not being used.

Better:

```csharp
public async Task<IActionResult> GetSomething(
    CancellationToken cancellationToken)
{
    var result =
        await service.GetSomethingAsync(
            cancellationToken);

    return Ok(result);
}
```

Then the service must continue propagating it.

---

## 13. Another common mistake

Do not create an unrelated token:

```csharp
var cancellationToken =
    new CancellationToken();
```

inside every method.

That token is not connected to the HTTP request.

Instead, accept the request token and propagate it:

```csharp
public async Task SomethingAsync(
    CancellationToken cancellationToken)
{
    await repository.DoSomethingAsync(
        cancellationToken);
}
```

---

## 14. Temporary MyLife test endpoint

For learning purposes, MyLife temporarily includes:

```text
GET /api/test/cancellation
```

The endpoint performs a 10-second delay:

```csharp
await Task.Delay(
    TimeSpan.FromSeconds(10),
    cancellationToken);
```

This allows us to observe what happens when the HTTP request is cancelled.

The endpoint is temporary and should be removed after verifying the behavior.

---

## 15. Key concepts learned

### CancellationToken

Represents a signal that cancellation has been requested.

### Cooperative cancellation

Code must observe the cancellation signal and stop appropriately.

### Request cancellation

ASP.NET Core can provide a token representing the lifetime of the current HTTP request.

### Token propagation

The same token should be passed from:

```text
Controller
    ↓
Application
    ↓
Infrastructure
    ↓
Database / external operation
```

### `ThrowIfCancellationRequested()`

Explicitly checks whether cancellation has been requested and throws `OperationCanceledException` if so.

### Cancellation is not necessarily an error

A client cancelling a request does not necessarily represent an application failure.

---

## 16. Interview explanation

If asked:

> What is CancellationToken and why do you use it in ASP.NET Core?

A good answer is:

> "CancellationToken provides co-operative cancellation for asynchronous operations. 
ASP.NET Core can provide a token representing the lifetime of the current HTTP request. 
I accept that token in the controller and propagate it through the application and infrastructure layers to operations such as EF Core queries or HTTP calls. 
If the client cancels the request, downstream operations that support cancellation can stop instead of continuing unnecessary work. 
This helps conserve server and database resources."

---

## 17. Current MyLife implementation

```text
MyLife.API
│
├── Controllers
│   └── HomeController.cs -> CancellationTest()   ← temporary
│
├── Middleware
│   └── GlobalExceptionHandler.cs
│
└── Program.cs
```

The cancellation test is temporary and will be removed after verification.
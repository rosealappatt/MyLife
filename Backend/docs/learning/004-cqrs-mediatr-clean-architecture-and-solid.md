# 004 — CQRS, MediatR, Clean Architecture, Design Patterns and SOLID

## 1. What is CQRS?

CQRS stands for Command Query Responsibility Segregation.

It separates application operations into two categories:

**Command**
- Represents an operation that initiates a change or action.
- Examples: RegisterUser, UpdateProfile, CreateMedicalRecord, Login.

**Query**
- Retrieves information.
- Does not modify application state.
- Examples: GetUserProfile, GetMedicalRecords, SearchRecords.

The main benefit is that read and write use cases can have different responsibilities and implementations.

CQRS does not require separate databases.

## 2. What is MediatR?

MediatR is a .NET library that implements the Mediator Pattern.

Without a mediator:

Controller -> Service -> Repository

With a mediator:

Controller -> MediatR -> Handler -> Dependencies

The controller sends a request. MediatR locates the registered handler and invokes it.

MediatR supports request/response, commands, queries, notifications and pipeline behaviours.

It does not itself implement business logic, persistence, authentication or distributed messaging.

## 3. Command lifecycle

Example: LoginCommand.

1. The API receives a login request.
2. The controller constructs a LoginCommand.
3. The controller sends it through ISender.
4. MediatR locates LoginCommandHandler.
5. The handler coordinates user retrieval and password verification.
6. The handler requests JWT creation through an application abstraction.
7. The handler returns a LoginResponse.

## 4. Query lifecycle

Example: GetCurrentUserQuery.

1. The API receives a request for the current user's profile.
2. The controller creates a GetCurrentUserQuery.
3. MediatR dispatches it to the query handler.
4. The handler retrieves the required information.
5. The handler returns a read response.

A query should not change persistent application state.

## 5. Clean / Layered Architecture

The projects have distinct responsibilities:

- API: HTTP and application entry points.
- Application: use cases and abstractions.
- Domain: core business rules.
- Infrastructure: database and external technology implementations.

The dependency direction is important.

Application should not depend on Infrastructure.

Infrastructure implements interfaces owned by Application.

Domain should remain independent of the other projects.

## 6. Vertical Slice Architecture

Vertical Slice organizes code by feature and use case instead of grouping every file of the same technical type together.

Example:

Features/
  Authentication/
    Commands/
      Login/
        LoginCommand.cs
        LoginCommandHandler.cs
    Queries/
      GetCurrentUser/
        GetCurrentUserQuery.cs
        GetCurrentUserQueryHandler.cs

This makes it easier to understand a feature without navigating many unrelated folders.

## 7. Other design patterns

### Mediator Pattern
Decouples the sender of a request from the handler that processes it.

### Repository Pattern
Abstracts persistence operations. It should be introduced where it provides meaningful separation, not automatically for every EF Core operation.

### Dependency Injection
Supplies required implementations to classes rather than making those classes construct their dependencies.

### Options Pattern
Maps configuration sections into strongly typed .NET classes.

### Strategy Pattern
Allows interchangeable implementations behind a common abstraction.

#
# The implementation will use:

- CQRS (stands for Command Query Responsibility Segregation.)
	Used in LoginCommand, GetCurrentUserQuery

- Mediator Pattern through MediatR.
	MediatR decouples the controller from the handler. 
	MediatR dispatches requests

- Repository Pattern where a persistence abstraction provides value.
	an abstraction over data access. We will use it where it provides value, not create a generic repository for every EF Core operation.

- Dependency Injection for implementation registration.
	Used in Register and supply implementations

- Options Pattern for strongly typed configuration.
	Used in JWT configuration

- Strategy Pattern where interchangeable implementations are useful.
	Lets us swap an implementation behind a contract without changing the caller. 
	For example, Password verification or token-related implementations behind abstractions.

- Dependency Inversion through application-owned abstractions.


## 8. SOLID principles

### Single Responsibility Principle
A class should have one clear reason to change.

Example:
- Controller handles HTTP.
- Login handler coordinates login.
- JWT service generates tokens.

### Open/Closed Principle
Software should allow extension without repeatedly modifying stable existing code.

Example: a token implementation can be replaced behind ITokenService.

### Liskov Substitution Principle
An implementation should honour the contract of its abstraction and be safely usable in place of it.

### Interface Segregation Principle
Prefer small, focused interfaces over large interfaces that force classes to implement unrelated operations.

### Dependency Inversion Principle
High-level application logic should depend on abstractions rather than infrastructure details.

Example:
LoginCommandHandler depends on ITokenService, not JwtTokenService.

# SOLID principles will guide the implementation, particularly Single Responsibility, Interface Segregation and Dependency Inversion.:
### Principle					Application in MyLife
S — Single Responsibility		- Controller handles HTTP; handler handles login use case; JWT service creates tokens.
O — Open/Closed					- We can add another token implementation without changing the login handler.
L — Liskov Substitution			- Implementations of an interface should be usable wherever that interface is expected.
I — Interface Segregation		- Prefer small contracts such as ITokenService rather than one enormous service interface.
D — Dependency Inversion		- Application depends on abstractions, not Infrastructure implementations.

## 9. CQRS is not the same as distributed messaging

MediatR dispatches requests inside the current application process.

A message broker such as Azure Service Bus or RabbitMQ supports communication between processes and services.

They solve different problems.

We will introduce distributed messaging only when MyLife has a real asynchronous integration or background-processing requirement.

## 10. Important interview questions

1. What is CQRS?
2. What is the difference between a command and a query?
3. Does CQRS require separate databases?
4. What is MediatR?
5. What is the Mediator Pattern?
6. Is MediatR required to implement CQRS?
7. What is the difference between MediatR and a message broker?
8. How does Dependency Inversion apply to Clean Architecture?
9. What is Vertical Slice Architecture?
10. What are the disadvantages of CQRS?
11. What is the Repository Pattern?
12. How would you implement validation and logging across all handlers?

## 11. Key takeaway

CQRS separates read and write use cases. MediatR dispatches requests to handlers. Clean Architecture separates responsibilities and protects application logic from infrastructure details. SOLID principles help keep these components maintainable.

These concepts complement one another, but they are not interchangeable.
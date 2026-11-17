# ADR 003 — CQRS and Application Architecture

- **Status:** Accepted
- **Date:** 2026-11-18
- **Project:** MyLife

## Context

MyLife is a full-stack application intended to demonstrate production-oriented .NET development and architectural concepts.

The application will grow to include authentication, user profiles, medical records, family access, expense tracking, reporting, and background operations.

A simple controller-service-repository structure could become difficult to maintain as the number of features and use cases grows.

We need an architecture that:

- Separates HTTP concerns from application behaviour.
- Separates read operations from write operations.
- Makes individual use cases independently understandable and testable.
- Supports dependency inversion.
- Allows infrastructure technologies to change without rewriting application logic.
- Provides practical experience with patterns commonly discussed in .NET interviews.

## Decision

MyLife will use Clean / Layered Architecture principles with CQRS and feature-based organization.

MediatR will be used as the in-process request dispatcher, subject to its applicable community licensing terms.

### Project responsibilities

**MyLife.API**
- HTTP controllers and endpoints.
- Request and response DTOs.
- Authentication middleware configuration.
- Dependency registration and application startup.

**MyLife.Application**
- Commands and queries.
- Command and query handlers.
- Application interfaces.
- Application-level validation and orchestration.
- Use-case-specific response models.

**MyLife.Domain**
- Domain entities.
- Value objects.
- Business rules and invariants.
- Domain events where justified.

**MyLife.Infrastructure**
- Entity Framework Core persistence.
- Repository implementations.
- JWT implementation.
- External service integrations.
- Email, queue and other technical integrations.

## CQRS approach

Each application use case will be represented as a command or query.

Commands represent operations that initiate a change or action.

Queries retrieve information without changing application state.

Each command or query will have a corresponding handler.

MediatR will dispatch requests to their handlers.

CQRS will initially use the same database and application deployment. Separate read/write databases are not required.

## Feature organization

Application features will use vertical-slice organization.

Example:

`Features/Authentication/Commands/Login/`

This folder will contain the login command, handler and relevant response model.

Related authentication queries will be organized under:

`Features/Authentication/Queries/`

## Other design patterns and principles

The implementation will use:

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


SOLID principles will guide the implementation, particularly Single Responsibility, Interface Segregation and Dependency Inversion.:
### Principle					Application in MyLife
S — Single Responsibility		- Controller handles HTTP; handler handles login use case; JWT service creates tokens.
O — Open/Closed					- We can add another token implementation without changing the login handler.
L — Liskov Substitution			- Implementations of an interface should be usable wherever that interface is expected.
I — Interface Segregation		- Prefer small contracts such as ITokenService rather than one enormous service interface.
D — Dependency Inversion		- Application depends on abstractions, not Infrastructure implementations.

## Alternatives considered

### Traditional controller-service-repository

Rejected as the primary application organization because application use cases could become scattered across large service classes.

### CQRS without MediatR

Technically valid and avoids an external mediator library, 
but MediatR provides useful practical experience with request dispatching and pipeline behaviours.

### Full distributed CQRS

Rejected for the initial implementation because separate databases, 
event synchronization and distributed messaging would add complexity without a current business requirement.

### Microservices

Rejected because MyLife is initially a single application. 
The architecture should not introduce distributed-system complexity prematurely.

## Consequences

### Benefits

- Clear separation of responsibilities.
- Independently testable use cases.
- Easier navigation by feature.
- Better understanding of CQRS and Mediator Pattern.
- Easier addition of validation, logging and transaction pipeline behaviours.
- Infrastructure can be replaced behind application contracts.

### Trade-offs

- More files and abstractions for simple operations.
- Additional learning and setup overhead.
- MediatR introduces an external dependency and licensing considerations.
- CQRS does not automatically improve performance or require separate databases.

## Future considerations

- Add validation pipeline behaviour.
- Add logging and performance pipeline behaviours.
- Introduce transaction behaviour when a use case requires atomic persistence.
- Use notifications for appropriate in-process events.
- Add asynchronous messaging only when a genuine background or integration requirement exists.
- Reassess repository abstractions as persistence requirements evolve.

## Decision summary

MyLife will use Clean / Layered Architecture, feature-based organization and CQRS with MediatR. 
The initial implementation will remain a modular monolith using a shared database, with no unnecessary distributed infrastructure.
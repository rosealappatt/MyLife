# ADR-001: Project Structure

## Status
Accepted

## Context

MyLife is being developed as an ASP.NET Core Web API
using .NET 10.

The application needs to demonstrate separation of concerns
between the API, application/business logic, domain model,
and infrastructure/database concerns.

## Decision

The solution will contain four projects:

- MyLife.API
- MyLife.Application
- MyLife.Domain
- MyLife.Infrastructure

...

## Dependency Direction

MyLife.API
    ↓
MyLife.Application
    ↓
MyLife.Domain

MyLife.Infrastructure
    ↓
MyLife.Application
    ↓
MyLife.Domain

## Consequences

...
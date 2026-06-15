# 3. MediatR-based CQRS handlers

- Status: Accepted
- Date: 2026-03-12
- Deciders: Backend leads
- Tags: application-layer, cqrs, mediator

## Context and Problem Statement

Controllers in the API project were starting to accumulate business logic and service-locator-style
constructor signatures. We needed a discipline that:

- Forces business logic out of controllers and into a testable application layer.
- Encourages one handler per use case, keeping classes small.
- Supports pipeline behaviours (validation, logging, transactions) without modifying every handler.

## Decision Drivers

- Test isolation (unit-testable use cases without ASP.NET hosting).
- Readability of feature folders (a feature = a folder of commands, queries, handlers, validators).
- Pipeline extensibility (cross-cutting concerns layered uniformly).

## Considered Options

1. **MediatR** with CQRS feature folders (`Features/<Aggregate>/<UseCase>/`).
2. Plain application services with constructor-injected dependencies.
3. A homegrown dispatcher.
4. MassTransit's in-memory mediator.

## Decision

We adopt **Option 1**: MediatR with CQRS folder convention.

- Every external entry point (controller action, Hangfire job, integration webhook) does only argument
  shaping and `_sender.Send(command)`. No business logic in controllers.
- Commands mutate state and may publish domain events; queries are read-only and return DTOs.
- Pipeline behaviours register cross-cutting concerns in this order: `Logging -> Validation (FluentValidation) -> Transaction -> Tenant scope -> Handler`.
- No service-locator. Handlers receive their collaborators via constructor injection.

## Consequences

- Positive: each use case is a single class with a single `Handle` method, trivially unit-testable.
- Positive: cross-cutting concerns (validation, transactions, logging) live in pipeline behaviours, not in handlers.
- Positive: feature folders read top-to-bottom: command + validator + handler + response DTO.
- Negative: extra indirection for trivial use cases. Acceptable; uniformity wins over micro-optimisation.
- Negative: MediatR licensing changed to commercial in late 2024. We pinned 12.x (last MIT version) in `Directory.Packages.props`; replacement (e.g. `Mediator` source generator) is tracked as a future ADR if needed.

## Links

- Application layer entry: `server/src/CoreAlign.Application/`
- Pipeline behaviours registered in `ApplicationServiceRegistration.cs`.

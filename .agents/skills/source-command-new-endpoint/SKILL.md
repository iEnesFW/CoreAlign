---
name: source-command-new-endpoint
description: Implement a new CoreAlign API endpoint through CQRS, tenant isolation, validation, integration tests, frontend hooks, and i18n. Use when the user invokes new-endpoint or asks to add a controller route, command/query endpoint, portal API operation, or corresponding frontend data flow in CoreAlign.
---

# CoreAlign New Endpoint

1. Read `AGENTS.md`, `docs/INVARIANTS.md`, and the matching §0.1 module row with all listed prerequisites.
2. Add the Application command or query, FluentValidation validator, DTO, handler, and at least one success and one failure test. Never return domain entities directly.
3. For money, stock, status, or other state mutation, implement `ITransactionalRequest`, durable idempotency, audit, and outbox behavior as required by §3.9 and §16.
4. Keep the controller body slim: bind, dispatch, return. Apply `[Authorize]` deliberately, return `ApiResponse<T>`, and throw mapped exceptions instead of encoding status behavior or leaking exception details.
5. Confirm all queries remain tenant-filtered. A missing `GetById` target must raise `NotFoundException`; never return a successful null response.
6. Apply the correct concurrency mechanism from §4.6 and `decimal(18,4)` for money.
7. Add integration coverage for happy path, authentication rejection, cross-tenant isolation with an accepted 404 or 403 result, and a tight N+1 round-trip budget of 3 or 4.
8. Put frontend access in the correct surface's `features/<x>/api` and `hooks/use<X>Queries` layers. Do not call the API from a component.
9. Add all visible text through `t()` and update Turkish and English locale files together.
10. Run `$source-command-pre-ship` and report each gate.

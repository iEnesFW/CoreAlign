# Architecture Decision Records

CoreAlign uses lightweight [MADR](https://adr.github.io/madr/)-style Architecture Decision Records
to capture meaningful, long-lived technical decisions and the context that produced them.

## Why ADRs

- Make implicit architecture explicit and reviewable.
- Preserve the **why** behind a decision so future contributors can revisit it with confidence.
- Surface tradeoffs (what we did _not_ choose, and the reason).

## When to write one

Write a new ADR when a change:

- Locks the team into a new dependency, framework, or persistence model.
- Establishes or revises a cross-cutting pattern (auth, tenancy, error handling, eventing).
- Picks one of several reasonable alternatives where the choice is non-obvious.
- Constrains downstream code (e.g. "every handler must X").

Do **not** write an ADR for routine implementation details, bug fixes, or refactors that preserve behaviour.

## Workflow

1. Copy the most recent ADR as a starting template.
2. Number it sequentially (`NNNN-title-in-kebab-case.md`).
3. Default the status to `Proposed`. Promote to `Accepted` once the PR merges.
4. Reference the ADR number from the PR description.
5. Never edit an `Accepted` ADR in place: supersede it with a new ADR and link back (`Superseded by ADR NNNN`).

## Status Vocabulary

- `Proposed` — under discussion, may change.
- `Accepted` — current law of the land.
- `Deprecated` — no longer the recommended approach, but no replacement yet.
- `Superseded by ADR XXXX` — replaced by a newer ADR.

## Index

| #                                             | Title                                                    | Status   |
| --------------------------------------------- | -------------------------------------------------------- | -------- |
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions                            | Accepted |
| [0002](0002-multi-tenant-shared-database.md)  | Multi-tenant shared database with EF global query filter | Accepted |
| [0003](0003-mediatr-cqrs.md)                  | MediatR-based CQRS handlers                              | Accepted |
| [0004](0004-outbox-pattern.md)                | Transactional outbox with Hangfire drain                 | Accepted |
| [0005](0005-jwt-with-persona-claim.md)        | JWT bearer tokens with `persona` claim                   | Accepted |
| [0006](0006-questpdf-for-documents.md)        | QuestPDF for document rendering                          | Accepted |
| [0007](0007-hangfire-for-scheduled-jobs.md)   | Hangfire for scheduled and background jobs               | Accepted |
| [0008](0008-iyzico-for-tr-payments.md)        | Iyzico as the primary payment gateway for TR             | Accepted |

# 4. Transactional outbox with Hangfire drain

- Status: Accepted
- Date: 2026-03-18
- Deciders: Backend leads, Platform leads
- Tags: eventing, reliability, hangfire

## Context and Problem Statement

Several use cases must atomically (a) write to the database and (b) publish an integration event
(e.g. order confirmed -> send invoice email, payment captured -> issue PDF receipt). Naively calling
the email service or message broker from within the handler creates a dual-write problem:

- If the broker call succeeds but the DB transaction rolls back, downstream systems act on an event that never happened.
- If the DB transaction commits but the broker call throws, the event is lost.

## Decision Drivers

- Exactly-once _delivery_ is impossible; we aim for at-least-once delivery with idempotent handlers.
- We must never leak partial state to downstream systems.
- We do not want a heavyweight message broker (Kafka / RabbitMQ) in MVP infra.

## Considered Options

1. **Transactional outbox table** + Hangfire recurring drain job.
2. Two-phase commit across DB and broker.
3. Synchronous in-process eventing (no durability).
4. Adopt MassTransit + RabbitMQ immediately.

## Decision

We adopt **Option 1**:

- An `OutboxMessages` table lives in the same PostgreSQL database, written in the **same EF transaction** as the aggregate change.
- A Hangfire recurring job (`OutboxDrainJob`, every 5 seconds) reads pending rows, invokes the appropriate handler, and marks each row as dispatched.
- Dispatch handlers must be idempotent (keyed by the outbox message id).
- A separate Hangfire job retries failed rows with exponential backoff up to 24h, then quarantines them for manual review.

## Consequences

- Positive: atomicity guaranteed by the database — no dual-write window.
- Positive: zero new infra dependency; reuses PostgreSQL + Hangfire we already operate.
- Positive: easy to inspect (it's just a table) and replay (re-set `DispatchedAt = null`).
- Negative: latency of up to one drain interval (~5s) between commit and downstream effect. Acceptable for current use cases (email, PDF, webhook).
- Negative: not suitable for very high event throughput. We will revisit if any tenant breaches 10k events/min sustained.

## Links

- Outbox table mapping in `server/src/CoreAlign.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs` (read-only).
- Drain job in `server/src/CoreAlign.Application/Jobs/OutboxDrainJob.cs` (read-only).
- Processor in `server/src/CoreAlign.Application/Common/Outbox/OutboxProcessor.cs` (read-only).
- Related: ADR 0007 (Hangfire as job runner).

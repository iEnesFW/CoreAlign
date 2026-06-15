# 7. Hangfire for scheduled and background jobs

- Status: Accepted
- Date: 2026-03-15
- Deciders: Backend leads, Platform leads
- Tags: background-jobs, scheduling

## Context and Problem Statement

CoreAlign needs background processing for: outbox drain (ADR 0004), document rendering, scheduled
reports, email delivery, daily housekeeping (token cleanup, audit log rotation), and ad-hoc
admin-triggered re-indexing. We also want a single dashboard to view, retry, and cancel jobs.

## Decision Drivers

- Persistence-backed job state so a worker crash does not lose work.
- Built-in dashboard for ops visibility.
- First-class .NET integration with DI.
- Same PostgreSQL persistence we already operate (no new infra).

## Considered Options

1. **Hangfire** with the PostgreSQL storage provider.
2. **Quartz.NET** with ADO storage.
3. **Azure WebJobs / Service Bus**.
4. **Coravel**.

## Decision

We adopt **Option 1**: Hangfire on PostgreSQL.

- Job server is hosted in-process inside the API host for now; we will split it to its own worker process in a later sprint once load justifies it.
- The Hangfire dashboard is mounted at `/internal/hangfire` and gated behind the `PlatformAdmin` policy (ADR 0005).
- Recurring jobs are registered in code at startup, not via the dashboard, so the schedule is in version control.
- All jobs are idempotent and tenant-scoped via an explicit `ITenantContext` setup at job start.

## Consequences

- Positive: no new infra dependency. Reuses our existing PostgreSQL.
- Positive: out-of-the-box dashboard for retries, queues, and recurring-job visibility.
- Positive: Quartz-style cron expressions supported.
- Negative: Hangfire's PostgreSQL storage adds tables to the operational DB. Mitigated by isolating Hangfire tables in a dedicated `hangfire` schema.
- Negative: in-process worker today couples API CPU with background work. Acceptable while traffic is low; tracked for a future split.

## Links

- Hangfire wiring in `server/src/CoreAlign.API/Hangfire/` (read-only).
- Recurring jobs registered in `server/src/CoreAlign.API/Hangfire/RecurringJobsRegistration.cs` (read-only).
- Dashboard authorization filter in `server/src/CoreAlign.API/Hangfire/HangfireDashboardAuthorizationFilter.cs` (read-only).
- Related: ADR 0004 (outbox drain runs on Hangfire).

# 2. Multi-tenant shared database with EF global query filter

- Status: Accepted
- Date: 2026-03-10
- Deciders: Backend leads, Platform leads
- Tags: tenancy, persistence, security

## Context and Problem Statement

CoreAlign serves many small-to-medium Turkish businesses. Per-tenant database isolation (database-per-tenant
or schema-per-tenant) would be operationally expensive at our pricing point and would complicate cross-tenant
analytics. At the same time, leaking a single row across tenants would be catastrophic.

We need an isolation model that is:

- Cheap per tenant (we expect thousands).
- Default-safe: forgetting to filter must not leak data.
- Compatible with PostgreSQL + EF Core 10.

## Decision Drivers

- Cost-to-serve per tenant.
- Default safety against developer error.
- Backup, restore, and migration ergonomics.
- Per-tenant analytics simplicity.

## Considered Options

1. **Shared database, shared schema** with a `TenantId` column on every tenant-owned aggregate and an EF Core **global query filter** that hard-binds reads to `ITenantContext.CurrentTenantId`.
2. Schema-per-tenant in a shared PostgreSQL database.
3. Database-per-tenant.
4. Row-level security (RLS) inside PostgreSQL.

## Decision

We adopt **Option 1**:

- Every tenant-scoped aggregate inherits a base `TenantEntity` carrying a non-nullable `TenantId`.
- The `CoreAlignDbContext` registers a global query filter for every `TenantEntity` derivative that compares the column to `ITenantContext.CurrentTenantId`.
- Writes are validated by an EF interceptor that throws if a `TenantEntity` is saved without a tenant id or with a tenant id different from the current context.
- Cross-tenant administrative reads bypass the filter only via an explicit `IgnoreQueryFilters()` call inside a clearly-marked PlatformAdmin service.

## Consequences

- Positive: single database, single migration pipeline, one backup, one connection pool — cheap.
- Positive: developers cannot accidentally fetch another tenant's rows because the filter is **on by default** at the DbContext level.
- Positive: works seamlessly with the existing MediatR + repository layering.
- Negative: large tenants share a table with small tenants. Mitigated by per-tenant partitioning (later) and per-tenant index hints if needed.
- Negative: `IgnoreQueryFilters()` is a footgun. Mitigated with a Roslyn analyzer in CI and code review focus on its few usages.

## Links

- Implementation entry points (read-only for ADR purposes):
  - `server/src/CoreAlign.Domain/Common/TenantEntity.cs`
  - `server/src/CoreAlign.Infrastructure/Persistence/CoreAlignDbContext.cs`
- See also ADR 0005 (persona claim — separate axis from tenant id).

# Sprint 9 — Parallel agent coordination notes

Date: 2026-06-04

## Required NuGet packages not yet referenced (Group A territory)

`server/src/CoreAlign.Infrastructure/CoreAlign.Infrastructure.csproj` does not yet reference the
packages needed for the optional cache/storage backends below. Group A shipped the abstractions
and stubs; the concrete implementations throw `NotSupportedException` until the packages are added.

### INFRA-012 — Redis (StackExchange.Redis)

- **Package:** `StackExchange.Redis` (>= 2.8.x compatible with net10.0)
- **Used by:** `CoreAlign.Infrastructure.Caching.RedisDistributedCacheService`
- **Activation flag:** `Redis:Enabled=true` (default `false` -> falls back to `InMemoryDistributedCacheService`)
- **Optional companion:** `AspNetCore.HealthChecks.Redis` for `/health` probe; when missing, register a NoOp health check that reports healthy whenever `Redis:Enabled=false`.

### INFRA-015 — S3 (AWSSDK.S3)

- **Package:** `AWSSDK.S3` (>= 3.7.x)
- **Used by:** `CoreAlign.Infrastructure.Storage.S3FileStorage`
- **Activation flag:** `Storage:Provider=S3` (default `Local` -> falls back to `LocalFileSystemStorage`)
- **Config keys:** `Storage:S3:Bucket`, `Storage:S3:Region`, `Storage:S3:AccessKeyId`, `Storage:S3:SecretAccessKey`, `Storage:S3:PublicBaseUrl`

### INFRA-015 — Azure Blob (Azure.Storage.Blobs)

- **Package:** `Azure.Storage.Blobs` (>= 12.x)
- **Used by:** `CoreAlign.Infrastructure.Storage.AzureBlobFileStorage`
- **Activation flag:** `Storage:Provider=AzureBlob`
- **Config keys:** `Storage:AzureBlob:ConnectionString`, `Storage:AzureBlob:Container`, `Storage:AzureBlob:ContainerPerTenant`, `Storage:AzureBlob:PublicBaseUrl`

## Why these are stubs

Per parallel-agent protocol, `CoreAlign.Infrastructure.csproj` may not be edited from this agent
to avoid clobbering concurrent edits. When a maintainer is ready to enable a backend:

1. `dotnet add server/src/CoreAlign.Infrastructure package <Name>`
2. Replace the throwing bodies in the corresponding `*FileStorage.cs` / `RedisDistributedCacheService.cs`
   with the real client calls. Public surface (interface, ctor, tenant prefixing rules) is already
   wired so no other code needs to change.
3. Flip the activation flag in environment configuration and re-run the integration suite.

## Default behavior today

- `Redis:Enabled=false` -> `InMemoryDistributedCacheService` backs `IDashboardCacheService`,
  `ILookupCacheService`, and any future custom-report-data cache region.
- `Storage:Provider=Local` -> `LocalFileSystemStorage` wrapped by `VirusScanFileStorage` (existing
  Sprint 8 decorator). Tenant isolation is enforced by the `{tenantId}/{scope}/{file}` prefix that
  `LocalFileSystemStorage` already emits.

Both defaults pass the cross-tenant isolation acceptance tests added in Sprint 9.

## ERP-027 (Group C) — Phase51ProductImages migration deferred

`server/src/CoreAlign.Infrastructure/Persistence/Migrations/CoreAlignDbContextModelSnapshot.cs`
is shared with the parallel agent and the per-migration `.Designer.cs` files mirror the
full model snapshot (the most recent one is ~610 KB). Hand-authoring a migration plus a
matching Designer requires copying every entity into the snapshot, which conflicts with
the "no shared-file edits" rule for this agent.

### Work shipped by Group C without the migration

- Domain: `CoreAlign.Domain.Entities.Catalog.ProductImage` (`TenantEntity`, FK to `Product`,
  cascade delete from product).
- EF mapping: `ProductImageConfiguration` (snake-case `product_images` table, composite
  indexes on `(tenant_id, product_id, display_order)` and a filtered unique-ish
  `(tenant_id, product_id)` where `is_primary = true`).
- Repository: `ProductImageRepository` using `_context.Set<ProductImage>()` so no DbSet
  property is needed on `CoreAlignDbContext`.

### Required follow-up (any agent allowed to touch the snapshot)

1. From repo root: `dotnet ef migrations add Phase51ProductImages --project server/src/CoreAlign.Infrastructure --startup-project server/src/CoreAlign.API`
2. Inspect the generated `Up`/`Down` for the `product_images` table and the two indexes
   listed above; no manual SQL is required.
3. `dotnet ef database update --project server/src/CoreAlign.Infrastructure --startup-project server/src/CoreAlign.API`

Until the migration ships, the integration test that exercises the upload flow is
gated behind the `Storage:Provider=Local` path and the `product_images` table will not
exist at runtime — admin UI gracefully renders an empty gallery in that scenario.

### Build break in CustomReportsController (Group B territory, not Group C)

Resolved 2026-06-05: Group B renamed the Domain-side enum to `ReportDeliveryFormat`
(`CoreAlign.Domain.Entities.Reporting.ReportDeliveryFormat`) so there is no longer a
clash with `CoreAlign.Application.Reports.Common.ReportFormat`. Backend solution
builds clean with `-warnaserror`; full Application test suite (1200 tests) green.

## ERP-025 + ERP-026 (Group B) — Phase54ReportingScheduling migration shipped without Designer

Migration: `20260605000000_Phase54ReportingScheduling.cs` creates `report_definitions` +
`report_schedules` (both TenantEntity, snake_case). The matching `.Designer.cs` and the
update to `CoreAlignDbContextModelSnapshot.cs` were intentionally skipped to avoid
clobbering Group A / Group C edits on the shared snapshot. Required follow-up by any
agent allowed to touch the snapshot:

1. `dotnet ef migrations add Phase54ReportingScheduling --project server/src/CoreAlign.Infrastructure --startup-project server/src/CoreAlign.API`
2. Diff the auto-generated Up/Down against the hand-authored migration shipped here;
   they should be identical. If not, replace the hand-authored file with the generated
   one and re-run integration tests.
3. `dotnet ef database update --project server/src/CoreAlign.Infrastructure --startup-project server/src/CoreAlign.API`

Domain entities (`CoreAlign.Domain.Entities.Reporting.ReportDefinition` /
`ReportSchedule`) and their EF configurations are picked up by
`ApplyConfigurationsFromAssembly`, so repositories work via `_context.Set<T>()` without
DbSet properties on `CoreAlignDbContext`. Hangfire registers `report-schedules` hourly
via `RecurringJobsRegistration`.

## Group B / e-invoice — Untracked test build break

`server/tests/CoreAlign.Integration.Tests/Providers/EFatura/NilveraIntegrationTests.cs`
(untracked, added by another agent) does not compile against the current
`NilveraEFaturaProvider` signature: the production provider expects an
`EFaturaGetStatusRequest` (not a `string`), `CancelAsync` no longer accepts the
3-arg overload referenced, and the constructor was reduced from 6 args. The
solution-wide `dotnet build` therefore fails with 3 errors today. Group C scope
(Domain + Application + Infrastructure + API + Application.Tests) builds clean
with 0 warnings / 0 errors; the failure is owned by whichever agent shipped the
new integration test.

## TEST-003 (Group B) — Playwright not yet on root `package.json`

The root `package.json` is shared between Group A/B/C edits, so this agent did not add
the `@playwright/test` devDependency or the `e2e` script directly. The e2e harness
(`e2e/`) is fully wired and assumes `npx playwright test` is available. To enable:

1. `npm install --save-dev @playwright/test`
2. `npx playwright install --with-deps chromium`
3. Add `"e2e": "playwright test --config=e2e/playwright.config.ts"` to scripts.
4. Re-run `.github/workflows/ci.yml` — the `e2e` job already references those scripts.

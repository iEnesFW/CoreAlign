---
name: project_corealign_error_handling
description: 'CoreAlign error-handling mechanism rebuilt to Level 5 — DB error log, unified correlation, OTel wired'
metadata:
  node_type: memory
  type: project
  originSessionId: 15659ef5-8d9f-4e27-b974-4dbee8b4663d
---

CoreAlign error-handling pushed from Level 3 to **Level 5 / ~99%** (work done 2026-06-15..17). The user's core ask: persist every user-facing error to DB with full detail (page, source, correlation, stack) so admin can query when a user reports an issue.

**Centerpiece — DB error log:** `ErrorLogEntry : BaseEntity` (table `error_logs`, NOT tenant-filtered so PlatformAdmin sees all; TenantAdmin scoped via controller IDOR check). Written by `ErrorLogWriter` (singleton, own IServiceScopeFactory scope, never throws, field truncation, 5s linked CTS timeout, increments OTel counters). `ExceptionHandlingMiddleware.ShouldCapture` decides what persists (5xx always Error; 4xx Warning except Validation/Auth/NotFound/401/404). Client never sees stack — generic "An unexpected error occurred." for 5xx; full detail only server-side + DB.

**Correlation unification:** one id flows through `CorrelationIdMiddleware` (X-Correlation-Id header + Activity tag/baggage + Sentry scope tag) → `error_logs.CorrelationId` → response body `traceId` (via `ApiResponse : ITraceableResponse` + `CorrelationResultFilter` on success path) → Serilog. Client error reports (3 apps) attach `getLastCorrelationId()`.

**Client capture (all 3 apps):** root `src` uses richer `shared/errors/windowHandlers.ts` (dedupe+parse+toast+reportClientError); portals (customer-portal, b2b) use own `installGlobalErrorReporting()`. All POST `/api/v1/client-errors` (anonymous + `[EnableRateLimiting("client-errors")]` 20/min). Admin UI: `pages/admin/ErrorLogsPage.tsx`. Retention: `ErrorLogRetentionJob` daily 04:00, 90-day cutoff.

**Gap-9 fix (was the only MAJOR blocker):** OTel was authored but `AddCoreAlignOpenTelemetry()` was NEVER called → counters `errorlog_persisted_total`/`errorlog_write_failed_total` dead. Fixed in `Program.cs`: call `AddCoreAlignOpenTelemetry(builder)` after Sentry + `app.UseOpenTelemetryPrometheusScrapingEndpoint()` (guarded by `OpenTelemetry:MetricsEnabled`, placed before auth so /metrics is scrapeable).

**Remaining 4 MINOR residuals are NON-defects (do not "fix" in code):** (1) graceful MeterProvider flush — already handled by `OpenTelemetry.Extensions.Hosting` dispose→Shutdown→flush; (2) static Meter init order — safe in practice; (3) metrics cardinality — bounded by `http.route` templates, monitor at scale; (4) /metrics unauthenticated — by OTel design, restrict at ingress/network ACL (making it default-off would un-export the counters). These are deployment/ops, not error-handling defects.

Accepted-by-design: ProviderHealth/ProviderTestRunner surface `ex.Message` (admin-policy-gated + sandbox-only 409 on prod creds). Related: [[project_corealign_notification_integration]].

**Binding rules for future API/feature work (added 2026-06-17):** canonical developer guide = `D:\CoreAlign\docs\modules\error-handling.md` (contract, exception→HTTP table, DB error-log investigation runbook, frontend capture, checklists, file map). Rule summary in `D:\CoreAlign\CLAUDE.md` §3.4 (backend — rewrote the OUTDATED `{ "error": {...} }` shape to the real `ApiResponse<T>` envelope) + §2.4 (frontend safeRequest + client capture + correlation). Two invariants appended to `docs/INVARIANTS.md`: [BUILD] verify `Add*/Use*` extensions are actually called in Program.cs; [API] every user-facing error persists to error_logs via middleware + single correlation id, no controller try/catch leaking ex.Message.

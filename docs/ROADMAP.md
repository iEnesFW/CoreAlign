---
title: CoreAlign Roadmap
version: 1.0
generated: 2026-06-01
schema_version: 1
total_items: 112
priority_counts: { P0: 18, P1: 34, P2: 33, P3: 27 }
categories:
  - production-blocker
  - compliance
  - auth-identity
  - infrastructure
  - observability
  - erp-module
  - portal-feature
  - tech-debt
  - testing
  - documentation
  - localization
  - operational
  - nice-to-have
effort_legend:
  XS: < 0.5 day
  S: 0.5 – 2 days
  M: 2 – 5 days
  L: 1 – 2 weeks
  XL: > 2 weeks
status_legend:
  todo: not started
  in-progress: being worked on
  blocked: waiting on external dependency
  done: shipped
---

# CoreAlign ROADMAP

> Living document. Source of truth for "what's left until production-ready" and "what would
> turn this into a category-leading Turkish SaaS ERP." Designed to be parsed by a roadmap
> viewer UI in the admin app — every item follows the same `### [ID]` + bullet list shape.

## How to read this document

Every actionable item has the exact same structure so a viewer can render it as cards:

```
### [ID-001] Short title
- **Category**: production-blocker | compliance | auth-identity | infrastructure | observability | erp-module | portal-feature | tech-debt | testing | documentation | localization | operational | nice-to-have
- **Priority**: P0 | P1 | P2 | P3
- **Effort**: XS | S | M | L | XL
- **Dependencies**: [other-ID, ...] or none
- **Status**: todo | in-progress | blocked | done
- **Current state**: one-paragraph factual description with file refs
- **Target state**: one-paragraph factual description
- **Acceptance criteria**:
  - bullet
  - bullet
- **Notes**: optional context, links, gotchas
```

ID prefixes: `AUTH` (identity), `COMP` (compliance), `INFRA`, `OBS` (observability), `ERP`
(business module), `PORTAL` (customer/dealer SPA), `DEBT` (tech debt), `TEST`, `DOC`,
`LOC` (localization), `OPS` (CI/CD/deploy), `NICE`.

---

## 1. Executive summary

CoreAlign's _engineering_ is unusually mature for its visible scope: 24+ EF migrations,
208 backend tests, BCrypt cost 12, comprehensive `SecurityHeadersMiddleware` (HSTS+CSP+COOP+
CORP), rate-limited auth, correlation IDs, structured Serilog, audit log middleware,
multi-tenant query filter, persona-aware authorization, KVKK export/erase scaffold,
Iyzico Checkout Form integration, ETag client cache, output cache with tenant variance,
36 controllers, 5 backend domain projects, 3 React 19 SPAs (tenant admin + customer portal

- dealer portal), a 27-entity Glass Enclosure vertical, Cmd/K command palette, lazy-loaded
  routes, module-gated sidebar, 5-language i18n stub.

The _production-deploy and Turkey-compliance_ layers are weak. There is **no CI/CD**, **no
`Dockerfile` for the API or any SPA** (only a Postgres dev compose), **no `appsettings.
Production.json`**, the root `README.md` is the default Vite template, **email delivery is
a stub** (only logs), **no e-Fatura/e-Arşiv integration** (legal requirement for Turkish
SMEs), **MFA columns exist on User with no API**, **refresh-token reuse detection is
missing**, KVKK erasure is too narrow, and `ar/de/ru` locales are ~3 % translated even
though they're advertised in the language switcher.

This document inventories **112 distinct items** ordered by priority, with file-level
references so each item can be picked up cold. P0 = blocker for any paying production
customer. P1 = needed for B2B competitive parity. P2 = quality / nice-tier features. P3 =
long-horizon ambitions.

## 2. What's surprisingly already done (positive findings)

Bake this list into the README — these are real differentiators today and should not be
duplicated as new work.

- **Multi-tenant isolation** — `ITenantOwned` + `TenantEntity` + global query filter in
  `CoreAlignDbContext.ApplyTenantQueryFilters`, auto-stamp on `SaveChanges`.
- **Three personas** — `customer`, `dealer`, `tenant` JWT claim with policies enforced at
  controller level (`PersonaAuthorizationPolicies.cs`).
- **3-way order approval flow** — dealer creates → customer approves/rejects → submit.
- **Transactional outbox** — `OutboxProcessor` + `OutboxDrainBehavior` runs _after_
  commit; 6+ message handlers wired (GL posting, comment, subscription, dealer order
  approval, customer approval/rejection, order comment).
- **KVKK Article 11 endpoints** — `/api/v1/privacy/me/export` + `/api/v1/privacy/me/erase`
  (latter requires username confirmation; scope is too narrow but the wiring is right).
- **Iyzico Checkout Form** integration with webhook signature verification (constant-time
  HMAC-SHA1) + replay-protection table (`ProcessedWebhookEvent`).
- **Audit log middleware** — `ActivityLogMiddleware` writes every non-GET request to
  `ActivityLog` via bounded channel + `ActivityLogWorker` background drain.
- **Login audit** — `LoginAuditLog` records every Success/Failed/Locked/Disabled/Unverified
  attempt; survives transaction rollbacks via a separate commit.
- **Refresh-token rotation** — SHA-256 hashed, `ReplacedByTokenHash` chain, httpOnly +
  SameSite=Strict + Secure cookie scoped to `/api/v1/auth`.
- **Rate limiting** — composite partition key (`scope|userId-or-ip|path`), 8/min sliding
  for auth, 200/min fixed for global.
- **Security headers** — HSTS preload, full CSP (`default-src 'self'`), COOP, CORP,
  Referrer-Policy, Permissions-Policy, `Server` header stripped.
- **Health probes** — `/health/live` (process-only) + `/health/ready` (DB-tagged).
- **Output cache** — `ShortTenant` (30s) + `LookupTenant` (5min) policies vary by
  `Authorization` header.
- **In-process caching** — `DashboardCacheService` (tenant-keyed, 30s) +
  `LookupCacheService` (5min, prefix invalidation).
- **Frontend httpCache** — `src/shared/http/httpCache.ts` with ETag/304 revalidation,
  single-flight, localStorage persistence, regex `TTL_RULES`.
- **Module gating** — `useActiveModules` + `RequireModule` attribute; sidebar hides
  modules the tenant didn't subscribe to.
- **Master-data CRUD UI** — Brands, Categories, Customer Groups, UoMs, Tax Rates, Payment
  Terms, Price Lists, Tags — all editable in Settings.
- **Document numbering UI** — per doc type prefix/padding/format with live preview.
- **GL posting map UI** — admin can map every GL outbox event to a GL account.
- **Turkish Chart of Accounts** — Tek Düzen Hesap Planı seeded automatically
  (`TurkishChartOfAccountsSeed.cs`).
- **Cmd/K command palette** in tenant admin (`CommandPalette.tsx`).
- **5-language switcher** in navbar (EN/TR/DE/AR/RU — only EN+TR are 100% translated).
- **Email verification + password reset** flows fully wired in domain (tokens hashed,
  single-use, TTL) — only the _delivery_ (SMTP) is stubbed.
- **2FA schema ready** — `User.IsTwoFactorEnabled` + `TwoFactorSecretKey` columns; just
  need the API surface.
- **e-Fatura schema ready** — `Invoice.EInvoiceUuid` + `EInvoiceStatus` + `EInvoicePdfPath`
  - `RegisterEInvoice(...)` method exists; just no GİB integrator is plugged in.
- **Tevkifat support** — `TaxRate.IsWithholding` + `Invoice.WithholdingTotal` already
  computed in `Invoice.Recalculate()`.
- **`--migrate` CLI flag** — `dotnet run -- --migrate` runs migrations and exits cleanly
  so CI can chain a deploy.

---

# Priority 0 — Production blockers

These items prevent the product from being safely deployed to a paying customer.

## Authentication & Identity

### [AUTH-001] Refresh-token reuse detection

- **Category**: auth-identity
- **Priority**: P0
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: `RefreshTokenCommandHandler.Handle` throws `TokenExpiredException`
  when a revoked token is replayed. The token family (chained via `ReplacedByTokenHash`)
  remains valid. This is the textbook signal of a stolen refresh token; production-grade
  auth must invalidate the entire family and notify the user.
- **Target state**: Replay of a revoked refresh-token revokes the whole chain (walk
  forward via `ReplacedByTokenHash`), revokes all linked `UserSession` rows, writes a
  `LoginAuditLog` row with reason `RefreshTokenReuse`, and enqueues a `SecurityAlertEmail`
  notification.
- **Acceptance criteria**:
  - Unit test: rotate token A → B → C; replay A; assert B + C revoked and user session list empty.
  - Audit log row visible in admin Activity view.
  - Email enqueued (once email is wired — see INFRA-001).
- **Notes**: File `server/src/CoreAlign.Application/Auth/Handlers/RefreshTokenCommandHandler.cs:39`.

### [AUTH-002] 2FA (TOTP) enable/verify endpoints

- **Category**: auth-identity
- **Priority**: P0
- **Effort**: M
- **Dependencies**: INFRA-001 (email for backup codes), AUTH-005 (step-up for destructive ops)
- **Status**: todo
- **Current state**: `User.IsTwoFactorEnabled` + `User.TwoFactorSecretKey` columns exist
  but no API surface. `LoginCommandHandler` has no challenge step. No TOTP enrollment UI.
- **Target state**: TOTP via `Otp.NET` library. Enroll, verify, regenerate backup codes,
  disable (requires password). Login challenge step. Admin can require 2FA for `TenantAdmin`
  role.
- **Acceptance criteria**:
  - `POST /api/v1/auth/2fa/enroll` returns QR code URI + manual key + 10 single-use backup codes.
  - `POST /api/v1/auth/2fa/verify` activates 2FA on user.
  - `POST /api/v1/auth/2fa/disable` requires current password.
  - `LoginCommandHandler` returns `RequiresTwoFactor=true` when 2FA active; `POST /api/v1/auth/2fa/challenge` completes login.
  - Backup-code consumption marks the code used in `TwoFactorBackupCode` table.
  - Tenant admin setting `RequireTwoFactorForRoles` enforced.
  - Frontend: profile page enroll/disable flow with QR code (use `qrcode` npm package).

### [AUTH-003] Password policy hardening

- **Category**: auth-identity
- **Priority**: P0
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: `AuthValidators.cs` requires min 8 + upper/lower/digit/special. No
  password history. No HIBP compromised-password check. No max length so bcrypt 72-byte
  truncation is silent. Below NIST 800-63B's recommended 15+ for admin tier.
- **Target state**: Min length 12 (general) / 15 (TenantAdmin); reject top-1000 common
  passwords + HIBP k-anonymity API; enforce password history (last 5); explicit max
  length 72 (bcrypt safe).
- **Acceptance criteria**:
  - `PasswordHistory` table populated on every change.
  - Register / change-password / reset blocks reuse of last 5 passwords.
  - HIBP check passes on offline mode (graceful when API unreachable).
  - Validator tests for all rules.

### [AUTH-004] Password reset revokes active sessions

- **Category**: auth-identity
- **Priority**: P0
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: `ResetPasswordCommandHandler` updates the password hash but does
  NOT revoke active `RefreshToken` / `UserSession` rows. `ChangePasswordCommandHandler`
  correctly does this — reset must match.
- **Target state**: After password reset, all refresh tokens are revoked, all user
  sessions terminated, SecurityStamp rotated.
- **Acceptance criteria**:
  - Unit test: login → reset → old refresh-token rejected with `RefreshTokenRevoked`.

### [AUTH-005] Step-up MFA for destructive admin operations

- **Category**: auth-identity
- **Priority**: P0
- **Effort**: M
- **Dependencies**: AUTH-002
- **Status**: todo
- **Current state**: TenantAdmin can delete customers, void issued invoices, change GL
  posting maps with only a normal session. The `PrivacyHandlers.EraseMyAccount` requires
  username re-typed (a good pattern) — extend it.
- **Target state**: `[RequireRecentMfa(MaxAgeMinutes=5)]` attribute on destructive
  endpoints (delete tenant, void invoice, change GL posting, delete user, change roles,
  KVKK erase someone else's data). Returns 412 Precondition Required when stale; frontend
  re-prompts TOTP code.

## Compliance (Turkish + KVKK)

### [COMP-001] e-Fatura / e-Arşiv integration

- **Category**: compliance
- **Priority**: P0
- **Effort**: XL
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Invoice.EInvoiceUuid` / `EInvoiceStatus` / `EInvoicePdfPath` columns
  - `Invoice.RegisterEInvoice(...)` method exist but are never called. No UBL-TR 2.1 XML
    generation. No GİB SOAP/REST integration. No VKN/TCKN checksum validator. No KEP
    integration.
- **Target state**: `IElectronicInvoiceGateway` abstraction with a Veriban or KolayBi
  adapter (pick one — Veriban has the cheapest REST API; KolayBi has better SLA). On
  invoice `Issue()`, generate UBL-TR 2.1 XML, send to GİB through integrator, persist
  returned UUID + status into `Invoice.RegisterEInvoice(...)`. Async polling for status
  changes via Hangfire job (INFRA-006). VKN (10-digit) + TCKN (11-digit) checksum
  validators applied to Customer and Vendor create/update.
- **Acceptance criteria**:
  - Issue invoice → XML generated → integrator API called → UUID stored.
  - Invoice list shows e-Fatura status badge.
  - VKN/TCKN typed wrong fails validation at form submit.
  - Settings page section for entegratör credentials per tenant.
  - 4+ integration tests against integrator sandbox.
- **Notes**: This is a _legal_ requirement. 2026 e-Fatura threshold effectively covers
  every CoreAlign target customer (3M TRY revenue mandate has dropped year-over-year).
  Pick Veriban for cost (~₺200/ay base), KolayBi for support (~₺500/ay).

### [COMP-002] KVKK silme scope expansion

- **Category**: compliance
- **Priority**: P0
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `EraseMyAccountHandler` anonymizes the `User` row only. `Customer`,
  `CustomerAddress`, `CustomerContact`, `Vendor`, `LoginAuditLog.IpAddress`,
  `ActivityLog.IpAddress`, `RefreshToken.DeviceInfo`, `EmailVerificationToken` — none
  touched. Regulator will fail an audit.
- **Target state**: Comprehensive cascade. For the requesting user: anonymize linked
  `CustomerContact`, hash `IpAddress` in log tables older than 30 days, delete unused
  tokens. For a tenant-initiated "erase customer X" flow: anonymize the Customer +
  Addresses + Contacts + CustomerUser rows + their orders' billing/shipping snapshots.
  Preserve financial records (TTK 10-year retention) with anonymized owner names.
- **Acceptance criteria**:
  - `POST /api/v1/privacy/me/erase` zeroes out all PII in 7 tables.
  - `POST /api/v1/privacy/customers/{id}/erase` (TenantAdmin + AUTH-005) cascades.
  - `Customer.IsAnonymized` flag prevents re-edit.
  - Audit log entry per anonymization.
  - 5+ unit tests covering each table.

### [COMP-003] Consent capture & cookie banner

- **Category**: compliance
- **Priority**: P0
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Zero `UserConsent` table. No cookie banner. No Aydınlatma Metni or
  Gizlilik Politikası static pages.
- **Target state**: `UserConsent` entity (`purpose`, `version`, `capturedAtUtc`,
  `ipAddress`, `userAgent`, `withdrawnAtUtc`). Capture on register + on policy updates.
  Cookie banner component with categories (Strictly Necessary / Analytics / Marketing),
  decisions persisted client-side + posted to backend. Static legal pages at
  `/legal/aydinlatma-metni`, `/legal/gizlilik-politikasi`, `/legal/kullanim-kosullari`.
- **Acceptance criteria**:
  - Banner appears on first visit; choice persisted 12 months.
  - Withdrawing consent prevents analytics scripts loading.
  - Admin can publish a new policy version → users see re-consent prompt.
  - All 3 portals show the banner (admin + customer + b2b).

### [COMP-004] VKN/TCKN checksum validators

- **Category**: compliance
- **Priority**: P0
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Customer.TaxNumber` and `Vendor.TaxNumber` accept any string.
- **Target state**: FluentValidation rule chains validating VKN (10-digit, sum mod 10 ==
  last digit using Turkish algorithm) and TCKN (11-digit, two checksum rules) where
  applicable.
- **Acceptance criteria**:
  - Customer create with `1234567890` → 400 with "Geçersiz VKN".
  - Valid VKN (e.g., `1729051985`) accepted.
  - 6 unit tests (good + bad for VKN + TCKN + edge cases).

### [COMP-005] KDV (VAT) declaration / Ba-Bs / KDV1 export

- **Category**: compliance
- **Priority**: P0
- **Effort**: L
- **Dependencies**: COMP-001 (e-Fatura adapter familiarity helps)
- **Status**: todo
- **Current state**: `Invoice.TaxBreakdownJson` stores per-rate KDV breakdown. No
  declaration document or GİB export.
- **Target state**: `TaxDeclaration` aggregate (period + status + lines). Handlers for
  `BuildKdv1ForPeriod`, `BuildBaBsForPeriod`. GİB XML format exporters.
- **Acceptance criteria**:
  - Admin generates KDV1 for a month → XML downloadable.
  - Ba/Bs lists vendor/customer summaries above 5k TRY threshold per current GİB rule.

### [COMP-006] Column-level encryption for PII

- **Category**: compliance
- **Priority**: P0
- **Effort**: M
- **Dependencies**: INFRA-008 (secrets vault)
- **Status**: todo
- **Current state**: Tax numbers, IBANs, national IDs land in Postgres in cleartext.
- **Target state**: ASP.NET DataProtection backed by Azure Key Vault / AWS KMS. EF Core
  `ValueConverter<string,string>` per encrypted column. Start with `Customer.NationalId`,
  `VendorBankAccount.Iban`, `User.PhoneNumber`.
- **Acceptance criteria**:
  - Selecting `national_id` from psql returns ciphertext.
  - Reading via API returns plaintext to authorized callers.
  - Rotation runbook documented (`docs/runbooks/key-rotation.md`).

### [COMP-007] Privacy / ToS / DPO contact pages

- **Category**: compliance
- **Priority**: P0
- **Effort**: S
- **Dependencies**: COMP-003
- **Status**: todo
- **Current state**: Privacy page is a static GDPR disclosure stub; no DPO contact; no
  ToS; no Aydınlatma Metni; no public version of the policies.
- **Target state**: 4 markdown-driven legal pages (Aydınlatma Metni, Gizlilik Politikası,
  KVKK Başvuru Formu, Çerez Politikası) + DPO email + footer link block in all 3 portals.
- **Acceptance criteria**:
  - All pages reachable unauthenticated.
  - DPO contact `dpo@<tenant-domain>` configurable per tenant.

## Infrastructure & Operations

### [INFRA-001] Real email delivery (SMTP / SendGrid)

- **Category**: infrastructure
- **Priority**: P0
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `EmailService.cs` implements every `IEmailService` method as a
  `_logger.LogInformation(...)`. Password reset tokens generate but no email leaves.
  `EmailTemplate` entity exists but no renderer wired.
- **Target state**: `MailKit` SMTP adapter (sufficient for self-hosted Turkish customers
  using their own SMTP) **plus** a SendGrid/Mailgun adapter (cheap egress). Template
  rendering via `Scriban` reading from `EmailTemplate.BodyHtml`. Send happens through
  outbox so failures retry with backoff.
- **Acceptance criteria**:
  - `IEmailService` swappable via DI based on `Email:Provider` config.
  - Password reset emails arrive in <2 minutes (sandbox SMTP).
  - Tenant admin can edit any `EmailTemplate` and trigger a "Send test email".
  - 6 system events emit emails: register (verify), password reset, 2FA enable,
    invoice issued, order pending approval, comment received.

### [INFRA-002] API Dockerfile + multi-stage build

- **Category**: infrastructure / operational
- **Priority**: P0
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Only `docker-compose.yml` containing a Postgres dev container.
  No Dockerfile for the API or the three SPAs.
- **Target state**: Multi-stage `server/Dockerfile` (sdk → publish → chiseled runtime).
  Three SPA Dockerfiles (node build → nginx static). Root `Dockerfile` for orchestrator
  or a `docker-compose.full.yml` that brings up Postgres + API + 3 SPAs behind a Caddy
  reverse proxy.
- **Acceptance criteria**:
  - `docker compose -f docker-compose.full.yml up` boots all 4 services healthy.
  - API image < 200 MB.
  - SPA images < 50 MB each (nginx:alpine base).
  - `.dockerignore` excludes `node_modules`, `bin`, `obj`, `.git`, `dist`.

### [INFRA-003] GitHub Actions CI pipeline

- **Category**: infrastructure / operational
- **Priority**: P0
- **Effort**: S
- **Dependencies**: INFRA-002 (Docker for image push step)
- **Status**: todo
- **Current state**: No `.github/workflows/` directory. Quality enforcement depends on
  developer's local pre-commit + Husky.
- **Target state**: `ci.yml` runs on push + PR: `npm ci → lint → typecheck → vitest →
dotnet test → dotnet publish → build SPAs`. Cache npm + nuget. Fail PR on any step.
  `release.yml` triggered on tag push: build + push images to registry.
- **Acceptance criteria**:
  - PR fails when `dotnet test` fails.
  - PR fails when lint fails.
  - Time to feedback < 10 minutes.
  - Code-coverage reporter posts comment.

### [INFRA-004] `appsettings.Production.json` template + secrets inventory

- **Category**: infrastructure / operational
- **Priority**: P0
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: Only `appsettings.json` + `appsettings.Development.json`. No Staging
  or Production overlay. No documented env-var inventory.
- **Target state**: `appsettings.Production.json` with safe defaults (rate limit lower,
  log level Warning, CORS empty); accompanying `docs/secrets-inventory.md` listing every
  env var the API consumes; `.env.example` per SPA workspace.
- **Acceptance criteria**:
  - `dotnet run --environment Production` starts only when every required secret present.
  - Documentation lists each var, sensitivity tier, where to set it.

### [INFRA-005] Remove `.env` from git, replace with `.env.example`

- **Category**: infrastructure / operational
- **Priority**: P0
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: `d:\CoreAlign\.env` is committed and tracked with
  `VITE_RECAPTCHA_SITE_KEY` (low sensitivity but bad practice).
- **Target state**: `.env` git-ignored; `.env.example` committed with placeholder values.
- **Acceptance criteria**:
  - `git ls-files | grep -E '^\.env$'` returns nothing.
  - `.env.example` covers all 3 SPAs (or one each per workspace).

### [INFRA-006] Scheduled outbox drain + token cleanup jobs

- **Category**: infrastructure
- **Priority**: P0
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `OutboxDrainBehavior` only fires after a MediatR request commits. If
  the request that enqueued the message crashes between commit and drain (or if there is
  no follow-up request), the outbox message never processes. No cleanup of expired
  refresh tokens, password reset tokens, email verification tokens.
- **Target state**: Hangfire (with `Hangfire.PostgreSql` storage) for cron jobs. Recurring
  jobs every 30s: outbox drain (orphan messages). Daily 03:00: cleanup of expired tokens,
  expired sessions, anonymize old IPs in logs. Daily 04:00: dispatch failed
  `GlassNotification` retries. Dashboard UI restricted to TenantAdmin.
- **Acceptance criteria**:
  - Manually fail a request after `_uow.SaveChangesAsync` and before `_outboxSignal.Signal`;
    next 30s drain processes the orphan.
  - Stale refresh tokens older than `RefreshTokenLifetime + 7d` get deleted.
  - Hangfire dashboard reachable at `/jobs` (TenantAdmin only).

### [INFRA-007] OpenTelemetry: traces + metrics + log correlation

- **Category**: observability
- **Priority**: P0
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Serilog + correlation IDs only. No traces, no metrics endpoint, no
  error tracker.
- **Target state**: `OpenTelemetry.Extensions.Hosting` +
  `OpenTelemetry.Instrumentation.AspNetCore` + `OpenTelemetry.Instrumentation.EntityFrameworkCore`
  - OTLP exporter. Configurable OTLP endpoint. `Activity.Current.Id` already lands in
    error responses; align Serilog scope to W3C TraceContext so logs + traces correlate.
- **Acceptance criteria**:
  - One trace per HTTP request includes DB spans.
  - `/metrics` Prometheus endpoint exposes request count, latency histogram, EF Core
    query duration.
  - Configurable OTEL_EXPORTER_OTLP_ENDPOINT.

### [INFRA-008] Secrets vault integration

- **Category**: infrastructure
- **Priority**: P0
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Dev relies on `dotnet user-secrets`. Production has no documented
  pattern beyond env vars.
- **Target state**: Optional `Azure.Extensions.AspNetCore.Configuration.Secrets` and
  `Amazon.Extensions.Configuration.SystemsManager` packages, wired via
  `Configuration:VaultProvider` switch in appsettings.
- **Acceptance criteria**:
  - Setting `Configuration:VaultProvider=AzureKeyVault` + KV URI loads Iyzico keys + JWT
    secret + connection string from KV at startup.
  - Same for AWS SSM Parameter Store.
  - Runbook documents which secret names to populate.

### [INFRA-009] Error tracking (Sentry or Application Insights)

- **Category**: observability
- **Priority**: P0
- **Effort**: XS
- **Dependencies**: INFRA-007 (synergy)
- **Status**: todo
- **Current state**: Unhandled exceptions log to Serilog and respond with `traceId`. No
  aggregation, no alerting, no source maps for SPA errors.
- **Target state**: `Sentry.AspNetCore` + `@sentry/react` in all 3 SPAs. DSN per
  environment. Source maps uploaded on SPA build.
- **Acceptance criteria**:
  - Throwing in any controller surfaces in Sentry within 10 s.
  - SPA `throw` triggers Sentry capture with correct file:line via source map.
  - Release name = git SHA.

### [INFRA-010] Replace default Vite `README.md`

- **Category**: documentation / operational
- **Priority**: P0
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: Root README is the Vite scaffold template.
- **Target state**: Project overview + quickstart (`docker compose up && dotnet ef
database update && dotnet run -- --migrate && npm run dev`) + architecture diagram +
  links to `docs/`. Plus per-app READMEs in `apps/customer-portal/` and `apps/b2b/`.
- **Acceptance criteria**:
  - Cold-start guide reproducible by an outsider in < 30 minutes.

### [INFRA-011] Operational runbooks

- **Category**: documentation / operational
- **Priority**: P0
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Only `docs/DEPLOY_SUBDOMAINS.md` exists (one runbook for nginx/DNS/CORS).
- **Target state**: `docs/runbooks/` with at minimum:
  - `01-deployment.md` (image build, env config, healthcheck verification, smoke tests)
  - `02-rollback.md` (image rollback, EF migration rollback strategy)
  - `03-db-migration.md` (the `--migrate` flag, EF bundle, downtime, multi-replica races)
  - `04-incident-response.md` (severity matrix, on-call rotation, post-mortem template)
  - `05-backup-restore.md` (pg_dump strategy, restore drill cadence)
  - `06-disaster-recovery.md` (RPO/RTO targets, region failover)
  - `07-key-rotation.md` (JWT key + Iyzico keys + DB password)
- **Acceptance criteria**: 7 runbooks committed, each with a "Last verified" date.

# Priority 1 — High value (B2B competitive parity)

## Authentication & Identity

### [AUTH-006] Role CRUD + permission model

- **Category**: auth-identity
- **Priority**: P1
- **Effort**: L
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Role` entity + repository registered; only `ListRoles` exposed. No
  per-permission granularity — controllers do `Roles="TenantAdmin"` string check. A
  Salesperson role can't be created.
- **Target state**: `Permission` + `RolePermission` tables. Custom
  `PermissionRequirement` + `IAuthorizationHandler`. `[HasPermission("Invoice.Cancel")]`
  attribute on controller actions. Settings UI: Roles tab with create/edit/delete +
  permission matrix (checkbox grid). Seed predefined "TenantAdmin", "Salesperson",
  "Accountant", "Warehouse" roles.
- **Acceptance criteria**:
  - Admin creates "ReadOnly" role → assigns → user cannot POST anywhere.
  - Permission grid lists ~40 named permissions grouped by module.
  - 8+ unit tests for permission evaluation.

### [AUTH-007] OAuth / OIDC SSO (Google + Microsoft)

- **Category**: auth-identity
- **Priority**: P1
- **Effort**: L
- **Dependencies**: AUTH-006
- **Status**: todo
- **Current state**: Password auth only. No OAuth packages referenced.
- **Target state**: Google + Microsoft providers via
  `Microsoft.AspNetCore.Authentication.Google` /
  `.MicrosoftAccount`. First login from a domain that matches a Tenant.OidcDomain
  auto-provisions user and assigns default role. Subsequent logins reuse User row by email.
- **Acceptance criteria**:
  - "Continue with Google" button on login.
  - Domain mapping per tenant in Settings.

### [AUTH-008] Active sessions UI + log-out-all

- **Category**: auth-identity
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: `UserSession` table tracks sessions but no listing or revoke
  endpoint.
- **Target state**: Profile → Security → "Active sessions" list (device, IP, last seen,
  current marker, revoke button) + "Log out of all other sessions".
- **Acceptance criteria**:
  - `GET /api/v1/auth/sessions` returns current + others.
  - `DELETE /api/v1/auth/sessions/{id}` revokes one.
  - `POST /api/v1/auth/sessions/revoke-others` keeps only current.

### [AUTH-009] Resend email-verification + extended user CRUD

- **Category**: auth-identity / erp-module
- **Priority**: P1
- **Effort**: S
- **Dependencies**: INFRA-001
- **Status**: todo
- **Current state**: No "resend verification" endpoint. Users page (admin) can invite,
  toggle active, edit roles — but cannot edit name/email/phone, force password reset, or
  delete a user.
- **Target state**: New endpoints: resend verification, edit user details, force password
  reset, soft-delete user (User.IsDeleted column). UI extends `UsersSection.tsx`.

## ERP modules (backend gaps)

### [ERP-001] `PriceListItem` CRUD endpoints

- **Category**: erp-module
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Entity + EF config exist but no commands, handlers, or endpoints.
  Tenant can create a PriceList but cannot add lines to it — the entire
  customer-specific-pricing flow is half-built.
- **Target state**: `AddPriceListItemCommand`, `UpdatePriceListItemCommand`,
  `RemovePriceListItemCommand`, `ListPriceListItemsQuery`. Endpoints under
  `/api/v1/price-lists/{id}/items`. Validators for `MinQuantity <= MaxQuantity`,
  `DiscountPercent ∈ [0,100]`. Settings UI: PriceList editor with line grid.
- **Acceptance criteria**:
  - Tenant adds 10 products to a price list with tiered quantities.
  - Pricing service resolves correct row based on order quantity.
  - 5+ unit tests.

### [ERP-002] Returns / RMA workflow

- **Category**: erp-module
- **Priority**: P1
- **Effort**: L
- **Dependencies**: ERP-003 (credit-note flow)
- **Status**: todo
- **Current state**: Domain stubs exist: `OrderType.Return`, `OrderStatus.Returned`,
  `OrderLine.RecordReturn(qty, reason)`, `Invoice.CreditNoteId` column. No commands, no
  controller, no UI.
- **Target state**: `CreateReturnRequestCommand`, `ApproveReturnCommand`,
  `ReceiveReturnedItemsCommand`. State machine: Requested → Approved → Received →
  CreditNoted → Refunded. Stock movement reverses on Received. Credit note auto-issued
  on receipt.
- **Acceptance criteria**:
  - Customer/dealer in portal posts RMA request.
  - Tenant admin approves or rejects.
  - On Receive, stock returns to warehouse, credit note generated, customer ledger
    credited.

### [ERP-003] Credit / debit note workflow

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `DocumentSequence` types include `CreditNote` and `DebitNote` (so
  numbering is ready), `Invoice.CreditNoteNumber` column exists, but no commands or UI.
- **Target state**: `IssueCreditNoteCommand` (from RMA flow or standalone). UI: from
  invoice detail "Issue credit note" → modal with lines selected for credit. Posts a GL
  entry reversing the original sale.
- **Acceptance criteria**:
  - Credit note appears in invoice list with proper badge.
  - GL: DR Sales Returns, CR AR.

### [ERP-004] Quote → Order workflow

- **Category**: erp-module
- **Priority**: P1
- **Effort**: L
- **Dependencies**: none
- **Status**: todo
- **Current state**: No `Quote` entity. Sales pipeline starts at Order.
- **Target state**: `Quote` aggregate + `QuoteLine`. State: Draft → Sent → Accepted /
  Rejected / Expired. "Convert to Order" creates an Order in Draft status linked back to
  the Quote via `OrderSourceQuoteId`. Customer portal + dealer portal can request quotes.
  Validity period (`ValidUntilUtc`) with auto-expire job.
- **Acceptance criteria**:
  - Dealer drafts a quote, sends to customer.
  - Customer approves → order created.
  - Quote PDF downloadable.

### [ERP-005] StockCount cycle-count document

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `CountStockCommand` is a single-shot adjustment. No multi-line count
  document, no variance report.
- **Target state**: `StockCount` aggregate (header + lines). Workflow: Plan → Counting →
  Reconciliation → Posted. Compares counted vs system, posts adjustments per line. UI in
  Inventory page.

### [ERP-006] VendorPayment full CRUD + apply to bill

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Only `Search` + `Create` exposed. No `GetById`, `Update`, `Void`, or
  `ApplyToBill`. Customer-side `PaymentApplication` exists; AP side does not.
- **Target state**: Full parity with customer payments. `ApplyVendorPaymentCommand`
  records the application against a VendorBill. UI updates `VendorBillsPage`.

### [ERP-007] VendorBill update + 3-way match report

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: VendorBill can only be Created + Posted + Cancelled. Correcting a
  draft requires Cancel + Recreate.
- **Target state**: `UpdateVendorBillCommand` restricted to Draft. Dedicated 3-way match
  report endpoint: lists PO ↔ Receipt ↔ Bill mismatches.

### [ERP-008] DiscountRule entity (global, not Glass-only)

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: ERP-001
- **Status**: todo
- **Current state**: `DiscountRule` exists only inside `GlassEnclosure` namespace. No
  general promotional discount rules.
- **Target state**: Lift `DiscountRule` into core Pricing. Rules can be by customer
  group, product category, time window, quantity threshold. Applies in pricing service.

### [ERP-009] TaxRule entity (region/product-class based)

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `TaxRate` is flat. No rule-based resolution by product class or
  region.
- **Target state**: `TaxRule` aggregate. `IPricingService.ResolveTax(line, customer)` →
  returns applicable rate.

### [ERP-010] Tenant admin CRUD endpoints

- **Category**: erp-module
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Tenant` entity + repo exist but no admin controller. Tenants are
  created implicitly during register.
- **Target state**: `TenantsController` with List + Get + Update + Archive (super-admin
  only — new role above TenantAdmin called `PlatformAdmin`).

### [ERP-011] Direct invoice creation (without order)

- **Category**: erp-module / portal-feature
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Invoices are only created by converting an Order. No "New Invoice"
  button on the list page.
- **Target state**: `CreateStandaloneInvoiceCommand` + form modal. Useful for service
  invoices, advance payments, etc.

### [ERP-012] Multi-currency FX feed (TCMB)

- **Category**: erp-module / infrastructure
- **Priority**: P1
- **Effort**: M
- **Dependencies**: INFRA-006
- **Status**: todo
- **Current state**: `Currency` entity present. `Invoice.ExchangeRate` snapshotted at
  issue time. No automatic FX rate ingestion. No FX revaluation entries.
- **Target state**: Daily Hangfire job polls TCMB XML
  (`https://www.tcmb.gov.tr/kurlar/today.xml`) → upserts `ExchangeRate` table. Month-end
  job posts FX revaluation to TDHP 656/646 accounts.

### [ERP-013] Order revision / amendment workflow

- **Category**: erp-module / portal-feature
- **Priority**: P1
- **Effort**: L
- **Dependencies**: none
- **Status**: todo
- **Current state**: A Submitted order can't be edited; must cancel + recreate. In a 3-way
  approval flow this is awkward (dealer submits, customer wants a small change, dealer
  must cancel and start over).
- **Target state**: `RequestOrderAmendmentCommand` → revision row with proposed changes
  → counterparty approves/rejects → order updated. Full audit trail.

### [ERP-014] Order PDF / Invoice PDF (server-side)

- **Category**: erp-module / portal-feature
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `InvoicePrintView.tsx` uses browser print. No server-generated PDF.
- **Target state**: `IDocumentRenderer` abstraction with QuestPDF or DinkToPdf adapter.
  Templates: invoice, credit note, order confirmation, quote, packing slip. Download
  link from list + detail views in all 3 apps.
- **Acceptance criteria**:
  - One-click invoice PDF download from admin + customer + dealer portals.

### [ERP-015] Multiple shipping/billing addresses (customer)

- **Category**: erp-module / portal-feature
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `CustomerAddress` entity supports many addresses but order checkout
  picks the default; no UI to choose another address per order.
- **Target state**: Address selection step in new-order flow (customer + dealer portals).
  Manage addresses page in customer profile.

### [ERP-016] Credit limit display + block on order create

- **Category**: erp-module / portal-feature
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Customer.CreditLimit` exists; `CreditLimitExceededException` exists;
  validation only happens deep in the order flow.
- **Target state**: Show current `Customer.OutstandingBalance / CreditLimit` in dealer +
  customer order form. Soft-block (warning) at 80 %; hard-block at 100 % unless override
  permission.

### [ERP-017] Min order quantity per product / per customer

- **Category**: erp-module
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: No min-order-qty field.
- **Target state**: `Product.MinOrderQuantity` + override in `CustomerProductPrice`.
  Validator enforces on order create.

### [ERP-018] Saved order templates / recurring orders

- **Category**: erp-module / portal-feature
- **Priority**: P1
- **Effort**: M
- **Dependencies**: INFRA-006
- **Status**: todo
- **Current state**: No template entity.
- **Target state**: `OrderTemplate` aggregate (lines + customer + frequency). Hangfire job
  triggers periodic order generation. "Save as template" button in NewOrderForm.

### [ERP-019] Bulk CSV/Excel import (Customers + Products + COA)

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: No bulk import anywhere. Frontend has CSV export only.
- **Target state**: `IBulkImportService<T>` abstraction. UI: drag-drop CSV/XLSX,
  preview, error highlighting, dry-run mode. Per row: validate via existing
  FluentValidation rules; commit only fully-valid rows; produce error report.

## Portal features

### [PORTAL-001] Customer portal: invoice PDF download + pay invoice

- **Category**: portal-feature
- **Priority**: P1
- **Effort**: M
- **Dependencies**: ERP-014 (PDF), INFRA-001 (email receipt)
- **Status**: todo
- **Current state**: `InvoiceDetailPage` is read-only. No "Download PDF" or "Pay now".
- **Target state**: PDF download button. "Pay now" launches Iyzico Checkout Form for AR
  payment (separate from SaaS module billing). Payment recorded against the invoice.

### [PORTAL-002] Customer portal: profile edit + change password + 2FA

- **Category**: portal-feature
- **Priority**: P1
- **Effort**: S
- **Dependencies**: AUTH-002
- **Status**: todo
- **Current state**: Profile is read-only; no logout-all, no 2FA enroll, no password
  change in portal (only the admin app has it).
- **Target state**: Profile sections: Profile (name/phone/email), Security (password +
  2FA + sessions), Notifications (preferences).

### [PORTAL-003] Customer portal: notification bell

- **Category**: portal-feature
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Admin app has `NotificationBell` (178 LoC). Portals don't.
- **Target state**: Port `NotificationBell` into shared component or duplicate into both
  portal apps. Same UX.

### [PORTAL-004] Dealer portal: invoices page (read)

- **Category**: portal-feature
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Dealer has no invoices page at all. Dealer can place orders but
  can't see invoices issued to their managed customers.
- **Target state**: `/invoices` page in b2b portal, scoped to dealer's managed customers
  via `IPortalScopeService`. Backend: `GET /api/v1/dealer-portal/invoices`.

### [PORTAL-005] Dealer portal: profile + logout

- **Category**: portal-feature
- **Priority**: P1
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: b2b ProfilePage is 34 LoC and has no logout button.
- **Target state**: Match customer-portal profile parity. Add logout.

### [PORTAL-006] Dealer portal: commission / earnings page

- **Category**: portal-feature
- **Priority**: P1
- **Effort**: M
- **Dependencies**: ERP-020
- **Status**: todo
- **Current state**: No commission tracking anywhere.
- **Target state**: After ERP-020 lands, b2b portal shows commission earned per order +
  total YTD with statement download.

### [ERP-020] Dealer commission / markup tracking

- **Category**: erp-module
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: No commission/markup model on DealerAccount.
- **Target state**: `DealerAccount.CommissionPercent` + per-customer override in
  `DealerCustomerLink.CommissionPercent`. `DealerCommissionLedgerEntry` written on every
  shipped order. Tenant view + dealer view of earnings.

## Compliance

### [COMP-008] Entity-level audit log with before/after diffs

- **Category**: compliance / observability
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `ActivityLog` records HTTP envelope only (method/path/status). No
  who-changed-what at entity level. No tamper-resistance.
- **Target state**: `EntityAuditLog` table populated via EF Core `SaveChanges`
  interceptor (entity name, entity id, action, before-json, after-json, user, ts).
  Optional rolling SHA-256 hash chain. Admin UI: per-entity timeline ("Customer ABC was
  changed by X on Y").
- **Acceptance criteria**:
  - Editing Customer X writes 1 audit row with diff.
  - Order detail page shows audit timeline tab.

### [COMP-009] Activity log filters + date range + per-entity drill

- **Category**: compliance / portal-feature
- **Priority**: P1
- **Effort**: S
- **Dependencies**: COMP-008
- **Status**: todo
- **Current state**: `ActivityPage.tsx` chronological pagination only. No filters.
- **Target state**: Filter by user, method, status, date range, entity type, entity id.
  CSV export.

## Infrastructure / observability

### [INFRA-012] Distributed cache (Redis)

- **Category**: infrastructure
- **Priority**: P1
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Caches are `IMemoryCache`-only; will diverge across replicas in
  scale-out.
- **Target state**: `StackExchange.Redis` + `AddStackExchangeRedisCache`; per-tenant
  prefix; failover to in-memory on Redis outage.

### [INFRA-013] Backend ETag emission

- **Category**: infrastructure / tech-debt
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Frontend `httpCache.ts` stores ETags + sends `If-None-Match` but
  backend never emits ETags, so revalidation never fires. Half-built cache loop.
- **Target state**: Action filter computes SHA-256 of response body and emits `ETag`
  header for cacheable read endpoints. 304 returned when match.

### [INFRA-014] Kestrel body-size + FileStorage size alignment

- **Category**: tech-debt
- **Priority**: P1
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Kestrel.MaxRequestBodyBytes = 10 MB`; `FileStorageOptions.MaxBytesPerFile = 25 MB`.
  Uploads between 10 MB and 25 MB fail at Kestrel before reaching the storage layer with
  a confusing 413.
- **Target state**: Bind both to the same config key with sensible default 25 MB.

### [INFRA-015] S3 / Azure Blob adapter for IFileStorage

- **Category**: infrastructure
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Only `LocalFileSystemStorage`.
- **Target state**: `S3FileStorage` + `AzureBlobFileStorage` adapters. Provider selected
  via `FileStorage:Provider` config.

### [INFRA-016] Virus scan (ClamAV) on uploads

- **Category**: infrastructure
- **Priority**: P1
- **Effort**: S
- **Dependencies**: INFRA-002 (Docker)
- **Status**: todo
- **Current state**: No virus scanning.
- **Target state**: ClamAV daemon sidecar; `IFileStorage.SaveAsync` scans before write.

### [INFRA-017] Tenant integration test (cross-tenant query prevention)

- **Category**: testing / compliance
- **Priority**: P1
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: No integration test asserts global tenant filter strength.
- **Target state**: Integration test boots two tenants, switches `ITenantContextAccessor`,
  asserts zero leakage on every read endpoint.

# Priority 2 — Medium value (quality + UX)

## Testing & Quality

### [TEST-001] Test coverage for Accounting (financial criticality)

- **Category**: testing
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Zero tests for `AccountingController`, GL posting outbox, period
  close, trial balance. Financial correctness rests on hand-checking.
- **Target state**: Unit tests for GLPostingMap resolution, JournalEntry balanced check,
  Period close rejecting late posts. Integration test: create order → post → assert GL
  rows.

### [TEST-002] Test coverage for portals (currently 0)

- **Category**: testing
- **Priority**: P2
- **Effort**: L
- **Dependencies**: none
- **Status**: todo
- **Current state**: Zero tests in `apps/customer-portal` and `apps/b2b`.
- **Target state**: Vitest setup + 30+ component tests per portal (login, order create,
  approval modal, comments).

### [TEST-003] E2E tests with Playwright

- **Category**: testing
- **Priority**: P2
- **Effort**: L
- **Dependencies**: INFRA-002, INFRA-003
- **Status**: todo
- **Current state**: No E2E framework.
- **Target state**: Playwright workspace covering 5 critical flows: tenant
  register→login, create order→approve→ship, customer pays invoice, dealer creates
  order→customer approves, KVKK self-erase.

### [TEST-004] Test coverage for Shipments, Inventory commands, MasterData

- **Category**: testing
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: No `tests/Shipments/` or `tests/Inventory/`. MasterData entirely
  untested.
- **Target state**: 40+ new unit tests across these three areas.

### [TEST-005] Coverage threshold enforced in CI

- **Category**: testing
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: INFRA-003
- **Status**: todo
- **Current state**: `coverlet.collector` not in test project; vitest coverage v8 reporter
  configured but no threshold.
- **Target state**: Backend ≥ 60 %, frontend ≥ 50 %, both enforced in CI.

### [TEST-006] FluentValidation validators for missing commands

- **Category**: testing
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Order FSM commands, Inventory Transfer/Count/Produce, MasterData
  Create/Update, Accounting Period commands, Payments commands lack validators.
- **Target state**: Add validators with reasonable rules. Length caps, range checks,
  enum guards.

### [TEST-007] k6 / Artillery load test scripts

- **Category**: testing
- **Priority**: P2
- **Effort**: M
- **Dependencies**: INFRA-002
- **Status**: todo
- **Current state**: No load tests.
- **Target state**: k6 scripts in `tests/performance/` covering common read + write
  endpoints. Baseline numbers documented.

## Reporting & Exports

### [ERP-021] PDF / XLSX export across the board

- **Category**: erp-module
- **Priority**: P2
- **Effort**: M
- **Dependencies**: ERP-014 (renderer)
- **Status**: todo
- **Current state**: Only CSV (`shared/lib/exportCsv.ts`). No XLSX. PDF only via browser
  print on invoice.
- **Target state**: `ExcelExportService` (ClosedXML) + `IDocumentRenderer` (QuestPDF).
  Export buttons on every list page.

### [ERP-022] Inventory reports

- **Category**: erp-module
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Only `Dashboard` shows low-stock tile.
- **Target state**: Stock valuation, slow movers, ABC analysis, stock-by-warehouse
  summary, low-stock alert grid.

### [ERP-023] Accounting reports (Cash Flow + GL detail + AP aging on Reports page)

- **Category**: erp-module
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: Trial Balance + Balance Sheet + Income Statement exist; no Cash
  Flow, no GL detail report, AP aging is its own page but not on Reports page.
- **Target state**: Cash Flow (Direct / Indirect), GL detail report (filter by account
  - period), unify Reports page tabs.

### [ERP-024] Purchase reports

- **Category**: erp-module
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: No purchase analytics.
- **Target state**: Spend by vendor, on-time delivery, top vendors.

### [ERP-025] Custom report builder (drag-drop fields)

- **Category**: erp-module
- **Priority**: P2
- **Effort**: XL
- **Dependencies**: ERP-023
- **Status**: todo
- **Current state**: Hard-coded reports only.
- **Target state**: User picks fields + filters + groupings; saves a named report; can
  schedule/email it.

### [ERP-026] Scheduled/emailed reports

- **Category**: erp-module
- **Priority**: P2
- **Effort**: S
- **Dependencies**: ERP-025, INFRA-006, INFRA-001
- **Status**: todo
- **Current state**: No scheduled delivery.
- **Target state**: User schedules a saved report; Hangfire generates + emails PDF.

## Auth & Account

### [AUTH-010] Anonymous-friendly captcha on register/login (defense-in-depth)

- **Category**: auth-identity
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: reCAPTCHA wired on auth pages (per app config); only rate limiting
  on backend.
- **Target state**: Server-side verification of reCAPTCHA token in login + register +
  forgot-password.

### [AUTH-011] Lockout notification email

- **Category**: auth-identity
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: INFRA-001
- **Status**: todo
- **Current state**: Lockout silent.
- **Target state**: Send "Your account was locked due to too many failed attempts" email.

### [AUTH-012] JWT key rotation (kid claim)

- **Category**: auth-identity
- **Priority**: P2
- **Effort**: S
- **Dependencies**: INFRA-008
- **Status**: todo
- **Current state**: Single symmetric secret; no `kid`.
- **Target state**: `kid` claim, JWKS-style key set, dual-key rotation (accept old +
  current, sign with current).

### [AUTH-013] CAPTCHA replaces rate-limit-only protection on register

- **Category**: auth-identity
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: AUTH-010
- **Status**: todo

## ERP modules (medium)

### [ERP-027] Product image upload + gallery

- **Category**: erp-module
- **Priority**: P2
- **Effort**: M
- **Dependencies**: INFRA-015
- **Status**: todo
- **Current state**: `Product.MainImageUrl` is URL-only.
- **Target state**: `ProductImage` entity (id + product + url + order). Drag-drop
  upload, multiple images, drag to reorder, set primary.

### [ERP-028] Product variants UI

- **Category**: erp-module
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: `ParentProductId` + `VariantAttributesJson` columns exist but no UI
  to manage variants.
- **Target state**: Product detail "Variants" tab. Auto-generate SKUs.

### [ERP-029] Address book country-aware validation

- **Category**: erp-module
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Address fields free-text.
- **Target state**: Postal code regex per country; Turkish il/ilçe lookup.

### [ERP-030] Notification preferences page (per user)

- **Category**: erp-module / portal-feature
- **Priority**: P2
- **Effort**: S
- **Dependencies**: INFRA-001
- **Status**: todo
- **Current state**: Notifications via bell only; no opt-out, no channel selection.
- **Target state**: `UserNotificationPreference` table per (user, type, channel).
  Settings page in profile.

### [ERP-031] Logo file upload (currently URL-only)

- **Category**: erp-module
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: INFRA-015
- **Status**: todo
- **Current state**: Branding section has logo URL input.
- **Target state**: File picker → uploads to `IFileStorage` → URL saved.

### [ERP-032] Customer statement-of-account PDF

- **Category**: erp-module
- **Priority**: P2
- **Effort**: S
- **Dependencies**: ERP-014
- **Status**: todo

### [ERP-033] Customer merge / dedupe tool

- **Category**: erp-module
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: No way to merge duplicate customers.
- **Target state**: Wizard: pick survivor + duplicates → reassign orders/invoices →
  archive losers.

### [ERP-034] Tag attach/detach endpoint (CustomerTagLink)

- **Category**: erp-module
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: Tags can only be set via full PUT on Customer.
- **Target state**: `POST/DELETE /api/v1/customers/{id}/tags/{tagId}`.

### [ERP-035] Retire legacy `Subscription` / `SubscriptionPlan` entities

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Old entities still registered; superseded by `Module` / `TenantModule`
  / `SubscriptionOrder`.
- **Target state**: Confirm zero UI references; remove from DI, EF model, migration.

### [ERP-036] Restrict `DemoDataSeeder` even more + document accounts

- **Category**: tech-debt / documentation
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: Already gated to Development. Demo passwords hard-coded as
  constants.
- **Target state**: Document the accounts in `docs/onboarding.md`; require env var
  `Demo:Enabled=true` AND `IsDevelopment` to seed.

## Localization

### [LOC-001] Complete TR/EN parity check + missing-key linter

- **Category**: localization
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: INFRA-003
- **Status**: todo
- **Current state**: TR and EN claim parity (~1937 keys); no automatic check.
- **Target state**: CI script `scripts/check-i18n-parity.ts` fails build on any key in
  one file but not the other.

### [LOC-002] Hide ar/de/ru until translated or commit to translating

- **Category**: localization
- **Priority**: P2
- **Effort**: S (hide) / L (translate)
- **Dependencies**: none
- **Status**: todo
- **Current state**: 5 languages advertised; ar/de/ru are ~3 % translated. Users get
  English fallback for ~97 % — looks broken.
- **Target state**: Either (a) hide ar/de/ru from language switcher until ≥ 95 %
  translated, or (b) commission translations.

### [LOC-003] RTL support (Arabic)

- **Category**: localization
- **Priority**: P2
- **Effort**: M
- **Dependencies**: LOC-002
- **Status**: todo
- **Current state**: No `dir="rtl"` toggle, no Tailwind RTL plugin.
- **Target state**: `tailwindcss-rtl` + `dir` flip on i18n change for Arabic.

### [LOC-004] Locale persistence to backend

- **Category**: localization
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo
- **Current state**: Language toggle only updates i18next memory.
- **Target state**: PUT user preference; load on login.

## Operational / Docs

### [OPS-001] CHANGELOG.md + SemVer + git tag conventions

- **Category**: operational / documentation
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo

### [OPS-002] PR template + CODEOWNERS + branch protection

- **Category**: operational
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: INFRA-003
- **Status**: todo

### [OPS-003] ADRs (Architecture Decision Records)

- **Category**: documentation
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: No ADRs. Architecture is implicit in code.
- **Target state**: `docs/adr/` with ADRs covering: multi-tenant strategy, DDD layer,
  outbox pattern, JWT vs cookie, persona policies, subdomain deployment, e-Fatura
  integrator choice.

### [OPS-004] Domain glossary

- **Category**: documentation
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo

### [OPS-005] Published OpenAPI spec + Redoc viewer

- **Category**: documentation
- **Priority**: P2
- **Effort**: S
- **Dependencies**: INFRA-003
- **Status**: todo

### [OPS-006] NSwag client generation for SPAs

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: S
- **Dependencies**: OPS-005
- **Status**: todo
- **Current state**: Frontend API types in `src/shared/types/api.ts` likely hand-maintained.
- **Target state**: `nswag.json` config; `npm run gen-api` produces types from running
  Swagger.

### [OPS-007] Favicon + PWA manifest + app icons

- **Category**: operational
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: none
- **Status**: todo

### [OPS-008] Performance budget (bundlesize / size-limit)

- **Category**: tech-debt / testing
- **Priority**: P2
- **Effort**: XS
- **Dependencies**: INFRA-003
- **Status**: todo
- **Current state**: SPA chunks already split via `vite.config.ts` manualChunks.
- **Target state**: `size-limit` config enforces per-chunk caps in CI.

## Tech debt (medium)

### [DEBT-001] PricingService.ResolveBatchAsync N+1

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Loop calls ResolveAsync per request → up to 3 round trips each.
  Catalog of 100 products = 300+ queries.
- **Target state**: Batch fetch all products + customer + price-list-items + CPP in 4
  queries; resolve in memory.

### [DEBT-002] Order composition 3× duplication (extract OrderAssemblyService)

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: M
- **Dependencies**: TEST-006 (validators) so refactor is safe
- **Status**: todo
- **Current state**: `CreateOrderCommandHandler`, `CreateDealerOrderHandler`,
  `CreateCustomerDirectOrderHandler` each repeat ~80 lines of orchestration.
- **Target state**: `IOrderCompositionService.AssembleAsync(...)` consolidates.

### [DEBT-003] Iyzico webhook ordering (Add before business commit)

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Gateway records `ProcessedWebhookEvent` before downstream handler
  transitions the order. If transition fails, event marked processed but order stuck.
- **Target state**: Move "Add" to after order state commit (accept at-least-once + use
  unique index for dedup).

### [DEBT-004] `Order.Recalculate()` rounds to 4dp (display-only rule violation)

- **Category**: tech-debt / compliance
- **Priority**: P2
- **Effort**: M
- **Dependencies**: TEST-001
- **Status**: todo
- **Current state**: `Math.Round(..., 4)` on Subtotal, LineDiscountTotal, TaxableTotal,
  TaxTotal, Total. CLAUDE.md global rule says decimals are display-only.
- **Target state**: Remove rounding from `Recalculate`; format only at presentation
  layer. Backfill test for invoice ≡ order total invariant.

### [DEBT-005] Order RowVersion concurrency token

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: M
- **Dependencies**: none
- **Status**: todo
- **Current state**: No optimistic concurrency on Order. Cancel↔approve race possible.
- **Target state**: `[Timestamp] byte[] RowVersion` on Order; SaveChanges raises
  `DbUpdateConcurrencyException`; handlers retry once with fresh load.

### [DEBT-006] Inventory dual-ledger reconciliation

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: L
- **Dependencies**: none
- **Status**: todo
- **Current state**: `Product.StockQuantity` decrements on Confirm; `StockItem.OnHand`
  on Allocate. Customer-direct + dealer-approved orders Submit-only — never Confirm in
  the auto-flow, so stock liability grows.
- **Target state**: Either auto-Confirm portal-initiated orders OR unify the ledger so
  Submit/Allocate keep both sides in sync.

### [DEBT-007] Catalog handler duplication (customer-portal + dealer-portal)

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: S
- **Dependencies**: ERP-001
- **Status**: todo
- **Current state**: `ListDealerCatalogProductsHandler` + `ListCustomerCatalogProductsHandler`
  share ~30 LoC.
- **Target state**: `CatalogProductPricingService` consolidates.

### [DEBT-008] Half-built `comment edit/delete` UX

- **Category**: tech-debt
- **Priority**: P2
- **Effort**: S
- **Dependencies**: none
- **Status**: todo
- **Current state**: Admin app has CommentsTab with edit/delete; portals don't.

# Priority 3 — Nice to have / long-horizon

### [NICE-001] Realtime chat (SignalR) replacing 30s polling

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: INFRA-012
- **Notes**: 30s polling is sufficient for MVP. Upgrade when typing-indicator / online
  presence becomes a requested feature.

### [NICE-002] File attachments on comments

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: INFRA-015
- **Notes**: Comment entity needs `Attachments` collection; existing IFileStorage adapter
  can persist.

### [NICE-003] @mentions in comments

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: none

### [NICE-004] SMS notifications (Twilio / NetGSM)

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: S
- **Dependencies**: INFRA-001 pattern
- **Notes**: Promote `INotificationChannelSender` out of Glass namespace; plug NetGSM
  (TR-friendly) or Twilio adapter.

### [NICE-005] Push notifications (FCM / WebPush)

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: NICE-004

### [NICE-006] WhatsApp Business API

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: NICE-004

### [NICE-007] Mobile apps (React Native)

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: XL
- **Dependencies**: AUTH-002 (mobile MFA)

### [NICE-008] White-label theming per tenant

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: ERP-031

### [NICE-009] Global search across portal (Cmd/K in portals)

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: none

### [NICE-010] Help center / docs / chatbot in portal

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: OPS-005

### [NICE-011] Onboarding wizard for new dealer/customer users

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: none

### [NICE-012] Dashboard widget drag-drop customization

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: none

### [NICE-013] In-product changelog popup

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: S
- **Dependencies**: OPS-001

### [NICE-014] Bulk actions on approvals (multi-approve / multi-reject)

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: S
- **Dependencies**: none

### [NICE-015] Saved filtered views / favorites

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: none

### [NICE-016] Loyalty / rewards (if B2C channel added)

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: none

### [NICE-017] Warranty / service request flow

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: ERP-002

### [NICE-018] GraphQL gateway for integrations

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: OPS-005

### [NICE-019] Per-tenant feature flag table beyond TenantModule

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: S
- **Dependencies**: none

### [NICE-020] Architecture tests (NetArchTest)

- **Category**: tech-debt / nice-to-have
- **Priority**: P3
- **Effort**: S
- **Dependencies**: INFRA-003

### [NICE-021] Hangfire dashboard restricted to TenantAdmin

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: XS
- **Dependencies**: INFRA-006

### [NICE-022] SOC 2 readiness audit

- **Category**: compliance / nice-to-have
- **Priority**: P3
- **Effort**: XL
- **Dependencies**: COMP-006, COMP-008, OPS-003

### [NICE-023] Per-tenant database isolation option

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: XL
- **Dependencies**: none
- **Notes**: Some enterprise customers refuse shared-schema; add per-tenant DB switch.

### [NICE-024] e-İrsaliye (e-Waybill) integration

- **Category**: compliance / nice-to-have
- **Priority**: P3
- **Effort**: L
- **Dependencies**: COMP-001
- **Notes**: Compulsory for goods movement > 100k TRY annual. Same integrator
  (Veriban/KolayBi).

### [NICE-025] KEP (Kayıtlı Elektronik Posta) integration

- **Category**: compliance / nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: COMP-001
- **Notes**: For legally binding e-mail.

### [NICE-026] GDPR DPA template + EU residency option

- **Category**: compliance / nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: none
- **Notes**: Defer until first EU lead.

### [NICE-027] WebAuthn (passkeys) as 2FA option

- **Category**: nice-to-have
- **Priority**: P3
- **Effort**: M
- **Dependencies**: AUTH-002

# Effort summary

| Priority  | XS  | S   | M   | L   | XL  | Total |
| --------- | --- | --- | --- | --- | --- | ----- |
| P0        | 4   | 6   | 7   | 0   | 1   | 18    |
| P1        | 2   | 13  | 14  | 5   | 0   | 34    |
| P2        | 11  | 14  | 8   | 0   | 0   | 33    |
| P3        | 2   | 6   | 9   | 7   | 3   | 27    |
| **Total** | 19  | 39  | 38  | 12  | 4   | 112   |

Rough calendar estimate (single full-time engineer, no parallelism, 1d/effort point):

- P0 only: ~7–10 weeks
- P0+P1: ~16–22 weeks (4–5 months)
- P0+P1+P2: ~24–32 weeks (6–8 months)
- P0+P1+P2+P3: ~50–65 weeks (12–15 months)

With 2 backend + 1 frontend + 1 fullstack engineers in parallel: divide by ~3 with some
coordination tax.

# Suggested sprint sequencing

## Sprint 1 — Hard production hardening (2 weeks)

Goal: deployable to first paying customer with critical security.

- [AUTH-001] refresh-token reuse detection
- [AUTH-003] password policy hardening
- [AUTH-004] password reset revokes sessions
- [INFRA-001] real email delivery
- [INFRA-004] appsettings.Production.json
- [INFRA-005] remove .env from git
- [INFRA-002] API Dockerfile
- [INFRA-003] GitHub Actions CI
- [INFRA-010] real README
- [OPS-007] favicon + manifest

## Sprint 2 — Compliance + observability (2 weeks)

Goal: legal-deployable in Türkiye.

- [COMP-001] e-Fatura integration (start, span Sprint 3)
- [COMP-002] KVKK silme scope expansion
- [COMP-003] consent + cookie banner
- [COMP-004] VKN/TCKN validators
- [COMP-007] privacy/ToS/DPO pages
- [INFRA-007] OpenTelemetry
- [INFRA-009] Sentry
- [INFRA-008] secrets vault
- [INFRA-011] operational runbooks (start)

## Sprint 3 — Finish compliance + B2B critical (2 weeks)

- [COMP-001] e-Fatura (finish)
- [COMP-005] KDV/Ba-Bs export
- [COMP-006] column encryption
- [AUTH-002] 2FA (TOTP)
- [AUTH-005] step-up MFA
- [ERP-014] PDF rendering
- [INFRA-006] Hangfire scheduler

## Sprint 4 — ERP gap closure (2 weeks)

- [ERP-001] PriceListItem CRUD
- [ERP-002] Returns / RMA
- [ERP-003] Credit / debit notes
- [ERP-013] Order amendment
- [ERP-011] Direct invoice creation
- [ERP-012] TCMB FX feed
- [ERP-019] Bulk CSV import

## Sprint 5 — Portal feature parity (2 weeks)

- [PORTAL-001] customer-portal invoice PDF + pay
- [PORTAL-002] customer-portal profile + 2FA
- [PORTAL-003] customer-portal notifications
- [PORTAL-004] dealer-portal invoices
- [PORTAL-005] dealer-portal profile + logout
- [ERP-015] multiple addresses
- [ERP-016] credit limit display
- [ERP-017] min order quantity

## Sprint 6 — Quality + reports (2 weeks)

- [ERP-004] Quote workflow
- [ERP-018] Saved order templates
- [ERP-020] Dealer commission
- [PORTAL-006] Dealer commission UI
- [COMP-008] entity-level audit log
- [COMP-009] activity log filters
- [TEST-001] Accounting tests
- [TEST-005] coverage thresholds

(Sprints 7+: P2 items, then P3 selectively per market signal.)

# Item index by category

- **production-blocker**: 18 items (all P0)
- **compliance**: COMP-001..009, NICE-022, NICE-024, NICE-025, NICE-026
- **auth-identity**: AUTH-001..013
- **infrastructure**: INFRA-001..017
- **observability**: INFRA-007, INFRA-009, COMP-008 (overlap)
- **erp-module**: ERP-001..036
- **portal-feature**: PORTAL-001..006 + ERP overlaps
- **tech-debt**: DEBT-001..008, ERP-035, ERP-036
- **testing**: TEST-001..007, INFRA-017
- **documentation**: OPS-003..006, INFRA-010, INFRA-011
- **localization**: LOC-001..004
- **operational**: OPS-001..008
- **nice-to-have**: NICE-001..027

# Source of findings

This document was synthesized from 5 parallel deep-dive audits (2026-06-01):

1. Backend module completeness — entities → handlers → controllers → tests, validator
   coverage, dead code, TODO scan.
2. Tenant-admin + portal frontend inventory — page-by-page operations matrix, stub
   detection, cross-cutting UI gap analysis.
3. Infrastructure & ops maturity — health, rate-limit, caching, jobs, observability,
   secrets, deployment.
4. Compliance & security posture — KVKK, e-Fatura, KDV, audit log, OWASP top 10,
   multi-tenancy isolation, MFA, password policy.
5. Operational readiness — CI/CD, docs, test coverage, localization, runbooks, branch
   strategy.

Each item's file references are accurate as of the audit date. Re-validate before
acting; codebase evolves.

---

**Maintenance notes**

- When an item ships, change `Status: todo → done` (don't delete) and add a
  `Shipped:` line with commit SHA + date.
- New items: follow the format strictly. Generate the next free ID per prefix.
- Quarterly: re-run the 5 audits and reconcile.

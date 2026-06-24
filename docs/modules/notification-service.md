# Notification Service — Architecture, Config & Runbook

> Status: 2026-06-16 · Owner: notification subsystem · Audience: backend, devops, tenant admins
> Scope: the in-process professional notification/email delivery engine, per-tenant SMTP, and customer/dealer document forwarding. Built on CoreAlign's existing provider-config + outbox + Hangfire infrastructure (no external microservice).

---

## 1. Executive Summary

CoreAlign delivers transactional notifications (Email/SMS/Push/WhatsApp/InApp) plus customer/dealer document forwarding. Delivery is **queue-first and reliable**: the dispatcher renders + persists a `NotificationMessage` and enqueues one outbox row per channel; a Hangfire drain sends each via the tenant's configured provider with **exponential backoff, dead-letter, and rate limiting**. SMTP is **per-tenant** (each tenant configures its own server + verified sender) with a platform fallback, managed from the admin UI.

Key properties: a per-channel failure never aborts sibling channels; sends are idempotent (SHA-256 content hash); credentials are encrypted at rest (DataProtection); the forward feature is IDOR-safe and rate-limited.

---

## 2. Architecture

```
Event/Command ──► INotificationDispatcher.DispatchAsync
                     │  render template (locale fallback tenant→global→en)
                     │  idempotency check (NotificationMessage by hash)
                     │  persist NotificationMessage = Queued (InApp = Sent inline)
                     └─ enqueue OutboxMessage("NotificationChannelSend") per channel/token
                                   │
        Hangfire "outbox-drain" (30s) ──► OutboxProcessor.DrainAsync
                     │  GetDueAcrossTenantsAsync (IgnoreQueryFilters + PushScope per row)
                     └─ NotificationChannelSendOutboxHandler
                            │  load message (tenant-scoped); skip if already Sent
                            │  resolve provider for tenant (IProviderRegistry<IEmailProvider> …)
                            │  rate-limit (INotificationRateLimiter, fixed window)
                            │  provider.SendAsync → MarkSent / MarkFailed
                            └─ result drives outbox: Processed | ScheduleRetry(backoff) | DeadLetter | Deferred(rate-limit)
```

- **Reliability core** (`CoreAlign.Application/Common/Outbox`): `OutboxRetryPolicy` (base 30s × 2^attempt, cap 30 min, full jitter); `OutboxMessage` gained `NextAttemptUtc`, `MaxAttempts` (default 8), `ScheduleRetry`/`DeferUntil`/`MarkDeadLetter`; `OutboxStatus.DeadLetter`. Two drain paths: the **inline** post-commit `OutboxDrainBehavior` calls `DrainCurrentTenantAsync` (tenant-scoped via the query filter — fast, immediate for what the request enqueued), while the **Hangfire** `outbox-drain` job calls `DrainAsync` (`GetDueAcrossTenantsAsync` with `IgnoreQueryFilters` + `PushScope` per row — the cross-tenant catch-up for retries/deferred). This split fixes a latent bug where a tenant-less drain matched no rows under the global query filter, without making every request drain all tenants.
- **Tenant-aware SMTP** (`CoreAlign.Infrastructure/Notifications/Email/TenantAwareSmtpEmailProvider`, MailKit): reads per-tenant encrypted credentials from `TenantProviderConfig` (Category=Email, ProviderName="smtp") via `IProviderCredentialProtector.UnprotectAs<SmtpCredentials>`, falls back to global `Notifications:Smtp`. Supports attachments, CC/BCC, Reply-To; `From` = tenant verified sender; never throws (returns `Fail`); has `CheckHealthAsync`.
- **Rate limiting**: `NotificationRateCounter` table, fixed 1-minute windows, unique index `(TenantId, ProviderName, Scope, ScopeKey, WindowStartUtc)` for concurrency safety. Scopes: per-tenant, per-provider, per-recipient.
- **Reused as-is**: `NotificationMessage`/template renderer/`NotificationTemplateSeeder`, `TenantProviderConfig` + resolver, DataProtection credential protector, transactional outbox + Hangfire(PostgreSql), `IDocumentService` scoped PDF renders.

> Note: a second email path (`IEmailSender` / `Common.Email.EmailMessage` / `SmtpEmailSender`, config section `Email`) is used by auth/security alerts. It is intentionally **left independent** to avoid changing which SMTP config those flows use; converging it onto the tenant-aware provider is a tracked follow-up.

---

## 3. Configuration

| Section                  | Keys                                                                                                      | Purpose                                                                     |
| ------------------------ | --------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `Notifications:Smtp`     | `Host, Port, UseSsl, Username, Password, FromAddress, FromName`                                           | Platform/global SMTP fallback used when a tenant has no SMTP config.        |
| `Notifications:Delivery` | `PerTenantPerMinute` (600), `PerProviderPerMinute` (300), `PerRecipientPerMinute` (20), `MaxAttempts` (8) | Rate limits + outbox max attempts before dead-letter. Validated on startup. |
| `Notifications:SendGrid` | `ApiKey, ApiBaseUrl`                                                                                      | Optional transactional email provider (capability: BulkSend).               |
| `Email`                  | `Smtp.*`                                                                                                  | Separate auth/security-alert path (independent — see §2 note).              |

Per-tenant SMTP credentials are stored encrypted in `tenant_provider_configs.EncryptedCredentialsJson` (never in appsettings). A tenant only sends email once it has an enabled, default Email provider configured (via the UI/API below); otherwise messages dead-letter with "No email provider configured" — by design.

---

## 4. Admin SMTP Settings (UI + API)

UI: **Dashboard → Administration → SMTP Settings** (`/dashboard/admin/smtp`, TenantAdmin only). Write-only password (blank = keep current), plus a "Send test email" card.

API (`[Authorize(Roles="TenantAdmin")]`, base `api/v1/admin/notifications/smtp`):

- `GET /` → settings (password never returned; `hasPassword` flag).
- `PUT /` → upsert (blank password preserved; wraps `UpsertTenantProviderConfigCommand`).
- `POST /test` `{ toAddress }` → send a fixed test email.
- `GET /health` → live connect/auth probe.

Resend a stored message: `POST api/v1/notification-messages/{id}/resend` (TenantAdmin) → re-queues a channel send.

---

## 5. Document Forwarding (customer & dealer)

Customers and dealers can forward **invoices and orders** as PDF to an external recipient.

- Endpoints: `POST api/v1/customer-portal/documents/forward` (customer persona) and `POST api/v1/dealer-portal/documents/forward` (dealer persona). Body `{ documentType: "Invoice"|"Order", documentId, recipientEmail }`, optional `Idempotency-Key` header.
- UI: "Forward by email" button on invoice/order detail pages in both `apps/customer-portal` and `apps/b2b`.
- **Security model**: the PDF is produced via `IDocumentService` **scoped renders** (`RenderInvoicePdfForCustomerAsync` etc.) which throw `*NotFoundException` (→404) on cross-scope access → **IDOR-safe**. `From` is the tenant's verified sender (never user-supplied); `Reply-To` is the forwarding user. Recipient is validated and CR/LF/`;`/`,` rejected (header-injection guard). Per-user + per-tenant rate limit → HTTP 429. Every forward is audited (`DocumentForwarded`) and idempotent.

Deferred: Quote forwarding (needs portal-scoped quote PDF renders + portal quote detail pages, neither of which exists yet) and free-form message body.

---

## 6. Data Model & Migration

Migration: `20260624000000_Phase80NotificationDeliveryEngine`.

- `outbox_messages`: add `next_attempt_utc timestamptz null`, `max_attempts int not null default 8`, index `(status, next_attempt_utc)`; backfills existing rows to `max_attempts = 8`.
- `notification_rate_counters`: new table + unique window index + `window_start_utc` index.

---

## 7. Operational Runbook

**First-time tenant setup**: TenantAdmin → SMTP Settings → enter host/port/credentials/from → Save → "Send test email" to confirm.

**Fresh-DB validation (post-merge / pre-deploy)** — run in your env:

```
dropdb corealign && createdb corealign
dotnet ef database update -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API
dotnet run -p server/src/CoreAlign.API            # confirm clean ValidateOnStart boot
```

Expect `notification_rate_counters` + the two new `outbox_messages` columns created; no `column already/does not exist`.

**Dead-letter replay**: failed deliveries dead-letter after `MaxAttempts` (8). Use the outbox admin replay (`ReplayOutboxCommand`) to requeue Deferred/Failed/DeadLetter rows once the cause (e.g. SMTP credentials) is fixed.

**Rate-limit tuning**: adjust `Notifications:Delivery:Per*PerMinute`. Counters auto-purge hourly via the `notification-rate-counter-cleanup` Hangfire job.

**Hangfire jobs**: `outbox-drain` (every 30s, drives all delivery), `notification-rate-counter-cleanup` (hourly). Dashboard `/hangfire` (TenantAdmin).

---

## 8. Verification Status

Build clean (0 warnings, `TreatWarningsAsErrors`). Backend unit tests for this subsystem: **61/61 green** — queue-first dispatch, per-channel handler, retry→dead-letter, rate-limit, and forward IDOR-negative + rate-limit. Admin SPA + both portals typecheck + lint clean. Migration emits valid SQL; API host boots cleanly (DI validated via the in-memory integration harness).

Pending (env-specific, run by operator): the fresh-DB + ValidateOnStart steps in §7 against real PostgreSQL.

---

## 9. Follow-ups (deferred)

- Multi-recipient batch send into one provider call (per-row queue is the current baseline; benefits bulk providers like SendGrid).
- OAuth2 / M365 (XOAUTH2) SMTP auth.
- Quote forwarding (scoped quote PDF renders + portal quote detail pages).
- Converge the `IEmailSender` auth-alert path onto the tenant-aware provider (single transport + single config source).

---
name: project_corealign_notification_integration
description: CoreAlign already has a full notification system; the standalone Omnisight NotificationService cannot be copied 1:1 into it
metadata:
  node_type: memory
  type: project
  originSessionId: 15659ef5-8d9f-4e27-b974-4dbee8b4663d
---

CoreAlign (D:\CoreAlign, net10.0 layered CQRS monolith, PostgreSQL) **already ships a full notification system** — do NOT assume it lacks one. It has 7 controllers (Notifications, NotificationMessages, MyNotificationMessages, NotificationPreferences, NotificationTemplates, NotificationWebhooks, ProfileNotifications), a real SMTP sender (CoreAlign.Infrastructure/Services/SmtpEmailSender.cs, MailKit 4.16.0), its own CoreAlign.Application.Notifications.NotificationDispatcher, Scriban templates (tenant→global→en fallback + seeder), SHA256 idempotency, and a transactional outbox + Hangfire(PostgreSql) drain.

The standalone Omnisight NotificationService source lives at **D:\NotificationService** (separate sln; runs in EMCM/Omnisight). It is a net8.0-windows microservice (MailKit/M365 email + webhook dispatcher, Hangfire.SqlServer queue, DPAPI credential protection, API-key auth, rate-limit/idempotency/dead-letter). It is fully self-contained (zero EMCM/CoreAlign references).

**Verdict (2026-06-15): do-not-copy-wholesale.** Not 1:1 compatible — 4 confirmed blockers: (1) net8.0-windows + global.json SDK 8.0.0 pin vs CoreAlign net10.0 + TreatWarningsAsErrors; (2) Windows-only DPAPI CredentialProtector vs CoreAlign DataProtection+Vault; (3) Hangfire.SqlServer/[Notification] schema vs CoreAlign Hangfire.PostgreSql 1.20.10; (4) NotificationService.Services.NotificationDispatcher name collides with CoreAlign's existing dispatcher.

What CoreAlign GENUINELY lacks (NotificationService has these): Hangfire queue retry/backoff, dead-letter, rate limiting, batch send, M365 OAuth2. CoreAlign's GlassEnclosure SMS/WhatsApp channel senders (NotificationChannelSenders.cs) are log-only stubs; email delivery is real.

**Decision taken:** user chose to BUILD the professional service IN-PROCESS (not the sidecar) — completed 2026-06-15. Approach B (port capabilities, not files). Plan file: C:\Users\enes.colak\.claude\plans\keen-doodling-spindle.md.

**What was implemented (all builds clean, net10, my 61 unit tests green):**

- Outbox reliability: OutboxStatus.DeadLetter, OutboxMessage.NextAttemptUtc/MaxAttempts(default 8)/ScheduleRetry/DeferUntil/MarkDeadLetter, OutboxRetryPolicy (exp backoff 30s→30min+jitter), OutboxProcessor now tenant-scoped drain via IOutboxRepository.GetDueAcrossTenantsAsync (IgnoreQueryFilters + PushScope per row — fixes the latent drain-never-runs bug) + backoff/dead-letter; OutboxAdmin replay includes DeadLetter.
- TenantAwareSmtpEmailProvider (Infrastructure/Notifications/Email, Name="smtp", MailKit): per-tenant creds from TenantProviderConfig(Email)+IProviderCredentialProtector.UnprotectAs<SmtpCredentials>, global SmtpEmailOptions fallback (now has FromAddress/FromName), attachments/Cc/Bcc/ReplyTo, never throws, CheckHealthAsync. Replaced old SmtpEmailProvider. EmailMessage grown with Cc/Bcc/Attachments+EmailAttachment. SmtpEmailSender (IEmailSender/auth-alert path, "Email" config section) left INDEPENDENT to avoid regression — convergence is a noted follow-up.
- Queue-first dispatch: NotificationDispatcher enqueues one OutboxMessage("NotificationChannelSend") per channel/token (InApp delivered inline) instead of inline send; per-channel NotificationChannelSendOutboxHandler does the actual provider send + idempotency re-check + rate-limit. Fixes rethrow-aborts-all-channels. NotificationRateCounter entity + RateScope + INotificationRateLimiter (fixed-window, unique-index concurrency-safe) + RateCounterCleanupJob (hourly). NotificationDeliveryOptions ("Notifications:Delivery"). NotificationRequest grew ReplyToOverride+Attachments.
- Per-tenant SMTP settings API: Application/Notifications/Smtp (Upsert/Get write-only password, SendTest, CheckHealth) wrapping UpsertTenantProviderConfigCommand; TenantSmtpSettingsController [TenantAdmin] route api/v1/admin/notifications/smtp. NotificationMessagesController resend stub → real ResendNotificationMessageCommand.
- Customer/dealer document forward: Application/Documents/Forwarding (ForwardCustomer/DealerDocumentCommand, IForwardDocumentService, validators reject CR/LF/;/,), reuses IDocumentService scoped renders (IDOR-safe), per-user+tenant rate limit (429 via DocumentForwardRateLimitExceededException : RateLimitExceededException mapped in ExceptionHandlingMiddleware), audit via IAuditContext, idempotency via Idempotency-Key header. Controllers: MyDocumentForwardController (customer-portal), DealerDocumentForwardController. Templates DocumentForward.Invoice/Order seeded (5 locales). From=tenant verified sender, ReplyTo=forwarding user.
- Migration: 20260624000000_Phase80NotificationDeliveryEngine (outbox cols + notification_rate_counters + indexes; backfills max_attempts=8). Generates valid SQL.
- Frontend: admin SPA D:\CoreAlign\src — features/admin/smtp + pages/admin/SmtpSettingsPage (useIsTenantAdmin gate, write-only password, test-send), route admin/smtp, sidebar Mail entry, Admin.Smtp i18n en+tr. Portal forward modal in apps/customer-portal + apps/b2b (features/portal/Forward*, wired on Invoice/Order detail pages, forward.* i18n en+tr). Portals use hand-written axios (no nswag regen). All typecheck+lint clean.

**NOT done (left for user):** DB tabula-rasa (DROP/CREATE + AutoMigrate) and API ValidateOnStart smoke — destructive/conflict-risk with running instance, run in controlled env. Also: multi-recipient batch send (per-row queue is the baseline), OAuth2/M365, free-form forward body, IEmailSender/Common.Email convergence, Quote forwarding (no portal-scoped quote PDF render yet).

**Concurrent-work caveat:** D:\CoreAlign had pre-existing uncommitted WIP (vendor-billing/purchasing/glass-enclosure/MRP, active commits). At hand-off their UpdateVendorBillHandlerTests didn't compile (their UpdateVendorBillHandler gained IProductRepository/IPurchaseOrderRepository params) — blocks the SHARED test project; NOT my code, left untouched. Linter reformats files on save and harness file-state cache can show stale content — verify with grep/build, not Read alone.

**Original sidecar option (not taken):** copy NotificationServiceClient.cs from Omnisight as a sidecar HTTP client.

**Hard constraint from user:** never modify the original NotificationService files at D:\NotificationService — only copy content. See [[project_emcm_backend_constraints]].

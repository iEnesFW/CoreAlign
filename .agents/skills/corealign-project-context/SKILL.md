---
name: corealign-project-context
description: Load historical CoreAlign architecture decisions for the design system, error handling and observability, or notification integration. Use when a CoreAlign task touches semantic UI tokens, landing/SEO conventions, error_logs and correlation, Sentry/OpenTelemetry wiring, client error reporting, SMTP, notifications, outbox delivery, document forwarding, or comparisons with D:\NotificationService.
---

# CoreAlign Project Context

Treat `AGENTS.md`, `docs/INVARIANTS.md`, current module documentation, and current code as authoritative. The files below are dated decision records; verify drift-prone facts before relying on them.

- For design system, frontend tokens, landing layout, SEO, typography, or canonical UI primitives, read `../../../.claude/memory/project_corealign_design_system.md`.
- For exception mapping, `error_logs`, correlation IDs, client error capture, Sentry, OpenTelemetry, metrics, or retention, read `../../../.claude/memory/project_corealign_error_handling.md`.
- For SMTP, notification dispatch, outbox retry/dead-letter, provider configuration, document forwarding, or the standalone Omnisight NotificationService, read `../../../.claude/memory/project_corealign_notification_integration.md`.
- Use `../../../.claude/memory/MEMORY.md` only when the task spans multiple recorded topics or needs the historical index.

After reading the relevant record, inspect the current files it names. Preserve explicit do-not-regress decisions, but do not repeat completed work or copy the standalone NotificationService wholesale. If the record conflicts with current code or documentation, report the drift and follow the current authoritative source.

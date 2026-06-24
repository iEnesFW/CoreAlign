# CoreAlign — Memory İndeksi

> Bu, canlı Claude Code memory store'unun **git-aynası indeksidir** (detay + senkron yönergeleri: `.claude/README.md`).
> **ÖNEMLİ:** Claude Code bu klasörü instruction olarak **OTOMATİK YÜKLEMEZ** — bağlayıcı kurallar `CLAUDE.md` (+ otomatik `@import` ile `docs/INVARIANTS.md`)'dedir. Buradaki dosyalar geçmiş iş kayıtları/kararlarıdır; kalıcı KURAL buraya değil `CLAUDE.md`/`INVARIANTS.md`'ye yazılır.

## Kalıcı kayıtlar

- [CoreAlign notification professional build (DONE)](project_corealign_notification_integration.md) — In-process pro notification service built 2026-06-15: queue-first dispatch + backoff/dead-letter/rate-limit, tenant-aware MailKit SMTP (TenantProviderConfig), per-tenant SMTP admin UI + test-send, customer/dealer document-forward (IDOR-safe), migration Phase80. Omnisight NotificationService NOT 1:1 copyable (kept untouched)
- [CoreAlign design system (Phase 1 DONE)](project_corealign_design_system.md) — brand=CoreAlign/indigo #6366f1; Tailwind @theme semantic tokens; fixed "NexusERP" bug; docs/DESIGN_SYSTEM.md; roadmap + pre-existing lint debt
- [CoreAlign error-handling Level 5 (DONE)](project_corealign_error_handling.md) — DB error_logs (admin-queryable, not tenant-filtered), unified correlation (header+body traceId+DB+Serilog+Sentry), resilient ErrorLogWriter, 3-app client capture, retention job; OTel wired in Program.cs (AddCoreAlignOpenTelemetry+/metrics); binding rules in repo CLAUDE.md §3.4/§2.4 + docs/modules/error-handling.md + docs/INVARIANTS.md

## Güncel notlar (2026-06-17)

- **Kural dosyaları yeniden yapılandırıldı.** `CLAUDE.md` yalın çekirdek + `@docs/INVARIANTS.md` **otomatik import** (defter artık her oturumda yüklü) + **§0.1 Modül Guardrail İndeksi** (dokunulan alanın tuzakları + "önce oku") + §0 dört-frontend (admin `src/` · `apps/customer-portal` · `apps/b2b` · Expo `mobile/`) & backend alt-sistem haritası + §3.9 MediatR pipeline sırası + §18 Claude Code düzeni. `GEMINI.md` → ince işaretçi; `docs/CLAUDE-additions.md` → emekli (içerik §11–17). `.claude/{commands,agents,hooks,settings.json}` eklendi.
- **Concurrency: bilinçli HYBRID (bir agent bunu bozmasın).** `IXminConcurrency` (Npgsql `xmin`, `ApplyXminConcurrencyTokens` + `Database.IsNpgsql()` guard'lı — finansal aggregate'ler) + `IHasConcurrencyToken` (app-managed `long`, SQLite test provider'ında da çalışır — ProductVariant/StockItem/FxRate/Glass\*). xmin unconditional = 161 SQLite testi kırılır (commit fc66c68). **StockItem concurrency Phase71 ile ÇÖZÜLDÜ** (ERP-CONCUR-001 kapandı). Detay: CLAUDE.md §4.6.

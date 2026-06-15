# Changelog

All notable changes to CoreAlign are documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).

Categories used per release: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.

## [Unreleased]

### Added

- Sprint 8 deliverables in progress (Operations / Documentation track):
  - CHANGELOG.md with SemVer + Keep-a-Changelog conventions (OPS-001).
  - Pull request template, CODEOWNERS, and branch protection guidance (OPS-002).
  - Architecture Decision Records (ADR) folder with meta-ADR plus 7 backfilled records (OPS-003).
  - Domain glossary covering ERP terminology in Turkish + English (OPS-004).
  - CI coverage threshold enforcement at 60% line coverage with override label workflow (TEST-005).

### Changed

- `.github/workflows/ci.yml` now collects Cobertura coverage and gates the build with `scripts/check-coverage.mjs`.

## [0.7.0] - 2026-05-22 — Sprint 7 (Stock module shipped)

### Added

- Stock pages: warehouse list/detail, stock items grid, unit-of-measure (UoM) management.
- Stock vouchers (in / out / transfer) with double-entry ledger movements.
- Reconciliation pass closing the dual-ledger drift between `Product.StockQuantity` and `StockItem` on Ship / Cancel paths (Allocate to Confirm reconciliation deferred).
- Customer portal: ability to view own warehouse on-hand for B2B persona.

### Changed

- DashboardCacheService keys partitioned by `(TenantId, UserId, WarehouseId)` to keep stock dashboards isolated per warehouse.

### Fixed

- Negative on-hand bug when a transfer voucher was posted before its counter-line existed (race in outbox).

## [0.6.0] - 2026-05-08 — Sprint 6 (Fire + Feedback modules)

### Added

- Fire incident reporting workflow with persona-scoped notifications.
- Customer feedback module (NPS + free text) with weekly aggregation Hangfire job.

### Changed

- IDocumentRenderer abstraction generalised so feedback PDF exports reuse QuestPDF pipeline introduced in ERP-014.

## [0.5.0] - 2026-04-24 — Sprint 5 (Inventory dual-ledger reconciliation)

### Added

- Inventory aggregate snapshot store for read-side dashboards.
- Allocate / Reserve commands for stock items.

### Fixed

- Ship and Cancel paths reconcile `Product.StockQuantity` with `StockItem` movements in a single transaction.

### Security

- TenantEntity global query filter audit pass: every aggregate now enforces tenant isolation in unit tests.

## [0.4.0] - 2026-04-10 — Sprint 4 (Returns, Credit Notes, Payments)

### Added

- Return Merchandise Authorisation (RMA) flow with reason codes.
- Credit note issuance tied to original invoice; signed PDF artifact.
- Iyzico payment integration for TR card payments with 3D-Secure callback.

### Changed

- Invoice numbering moved from auto-increment to per-tenant document numbering sequence.

## [0.3.0] - 2026-03-27 — Sprint 3 (Quotes, Orders, Invoices, PDF rendering)

### Added

- Quote -> Order -> Invoice state machine with MediatR command handlers.
- `IDocumentRenderer` + `QuestPdfDocumentRenderer` (ERP-014).
- ClosedXML-based XLSX exports for order book and invoice register.

### Changed

- All `/api/v1` endpoints now require Persona policy in addition to JWT validation.

## [0.2.0] - 2026-03-13 — Sprint 2 (Multi-tenant + Auth)

### Added

- TenantEntity + EF Core global query filter for tenant isolation.
- JWT bearer auth with `persona` claim (Customer / Dealer / Tenant / PlatformAdmin).
- Persona-based authorisation policies registered in `Program.cs`.
- Hangfire scheduled job infrastructure (outbox drain, daily cleanup).

### Security

- Refresh-token rotation; access token TTL reduced to 15 minutes.

## [0.1.0] - 2026-02-27 — Sprint 1 (Foundation)

### Added

- Solution skeleton: Domain / Application / Infrastructure / API layered projects.
- PostgreSQL + EF Core 10 baseline with initial migration.
- React 19 + Vite 7 + Tailwind v4 admin SPA scaffold.
- MediatR registration + CQRS folder convention.
- ICurrentUserAccessor + ITenantContext abstractions.
- safeRequest + useQuery hooks in admin SPA.

[Unreleased]: https://github.com/corealign/corealign/compare/v0.7.0...HEAD
[0.7.0]: https://github.com/corealign/corealign/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/corealign/corealign/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/corealign/corealign/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/corealign/corealign/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/corealign/corealign/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/corealign/corealign/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/corealign/corealign/releases/tag/v0.1.0

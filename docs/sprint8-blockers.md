# Sprint 8 — Parallel agent coordination notes

Date: 2026-06-04

## Out-of-scope build break (parallel agent territory)

**File:** `server/tests/CoreAlign.Integration.Tests/Providers/EFatura/NilveraIntegrationTests.cs`

**Errors (3, all CS):**

- Line 143 — `CS1503: cannot convert from 'string' to 'CoreAlign.Application.Providers.EFatura.EFaturaGetStatusRequest'`
- Line 160 — `CS1501: No overload for method 'CancelAsync' takes 3 arguments`
- Line 192 — `CS1729: 'NilveraEFaturaProvider' does not contain a constructor that takes 6 arguments`

**Root cause:** `NilveraEFaturaProvider` public surface (ctor arity, `GetStatusAsync` parameter type, `CancelAsync` overload) was changed; integration tests were not updated.

**Owner:** parallel agent working on e-fatura / Nilvera provider feature.

**Impact today:** `dotnet build CoreAlign.sln` fails. `dotnet test --no-build` still passes against cached DLLs (1064 Application + 55 Integration). Fresh builds blocked.

**Recommended fix:** update `NilveraIntegrationTests.cs` to call the current `NilveraEFaturaProvider` API:

- Pass an `EFaturaGetStatusRequest` to `GetStatusAsync` (not a raw string)
- Use the 2-arg `CancelAsync(invoiceUuid, reason)` overload
- Update the ctor call to the current arity (likely added one parameter — check the provider class)

## Sprint 8 deliverables (Groups A/B/C — this session, NOT touched by parallel agent)

### Group A — ERP Reports

- `IReportRenderer` + `ReportDocument` + `ReportFile` + `ReportFileFactory` (Application/Reports/Common/)
- `QuestPdfReportRenderer` + `ClosedXmlReportRenderer` + `ReportDataReader` (Infrastructure/Reports/)
- 9 reports: stock on hand, stock movements, reorder alerts, cash flow, GL detail, AP aging, purchase by vendor, purchase by product, open POs
- `ReportsController` generic endpoint `GET /api/v1/reports/{reportKey}?format=pdf|xlsx`
- Admin `ReportLibraryPage` with filter form + PDF/XLSX download
- +21 tests (renderer output, query filters, RBAC); XLSX formula injection sanitizer added during review pass

### Group B — Performance & Compliance v2

- `EtagMiddleware` (SHA-256 ETag on JSON/text GET responses; 304 on If-None-Match; skips file-download paths + Range requests + bodies >1 MB)
- `IVirusScanner` abstraction + `ClamAvVirusScanner` (INSTREAM TCP) + `NoOpVirusScanner`
- `VirusScanFileStorage` decorator (fail-closed in prod, fail-open in dev via NoOp default)
- `AuditTimeline` wired into 4 admin detail pages (Customer / Order / Invoice / Product)
- +26 tests (ETag middleware 14 + VirusScan 12)

### Group C — DX + Docs (zero source code changes)

- `CHANGELOG.md` (Keep-a-Changelog, Sprint 1-7 backfilled)
- `.github/PULL_REQUEST_TEMPLATE.md`
- `.github/CODEOWNERS`
- `docs/release-process.md`
- `docs/branch-protection.md`
- `docs/glossary.md` (34 entries, en+tr)
- `docs/adr/` — 8 ADRs (meta + 7 backfilled)
- `docs/coverage-policy.md`
- `scripts/check-coverage.mjs`
- `.github/workflows/ci.yml` extended with coverage-gate job (60% line threshold)

## Final cached state

- Backend src: 0 warn / 0 err (test project only blocks fresh build)
- Application.Tests: 1064 passing / 0 failed
- Integration.Tests: 55 passing / 0 failed
- Frontend typecheck: exit 0
- Frontend lint: post-auto-fix should be exit 0
- All 3 vite builds: green

# Coverage Policy

CoreAlign enforces a backend line-coverage gate in CI. This document explains the threshold, how
the gate is computed, how to read the report, and the **rare** circumstances under which a PR may
bypass the gate.

## 1. Threshold

- **Current threshold:** **60% line coverage** across the backend solution (`CoreAlign.sln`).
- The threshold is intentionally lenient at first; it will be raised in 5-percentage-point
  increments once the floor is comfortably exceeded. The intent is to enforce a **ratchet**:
  coverage must never regress below the active threshold.
- The threshold is defined in `.github/workflows/ci.yml` via the `--threshold` flag passed to
  `scripts/check-coverage.mjs`. Change it via a PR that also updates this document.

## 2. How the Gate Works

1. The backend test step runs with `--collect:"XPlat Code Coverage"`, which produces a
   `coverage.cobertura.xml` file under `./TestResults/<run-guid>/`.
2. A dedicated CI step runs `node scripts/check-coverage.mjs --threshold 60 --path ./TestResults`.
3. The script aggregates `lines-valid` and `lines-covered` across every Cobertura report it finds,
   computes the percentage, and exits non-zero if it is below the threshold.
4. A failed gate fails the workflow, which (under branch protection — see `docs/branch-protection.md`)
   blocks the PR from merging.

## 3. Reading the Report

The script's stdout looks like:

```
coverage: 64.27% (12854/20000 lines) across 7 report(s)
threshold: 60%
PASS: coverage meets the configured threshold.
```

For a richer per-file view locally:

```bash
dotnet test CoreAlign.sln -c Release --collect:"XPlat Code Coverage"
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./coverage-report -reporttypes:Html
# Open ./coverage-report/index.html
```

## 4. What Counts

- **Counted:** code under `server/src/CoreAlign.{Domain,Application,Infrastructure,API}`.
- **Excluded** (already configured via `[ExcludeFromCodeCoverage]` or `coverlet.runsettings`):
  - Generated code (EF Core migrations, NSwag clients).
  - Program startup wiring (`Program.cs`, `*ServiceRegistration.cs`).
  - DTOs and request/response records that have no behaviour.

Frontend coverage is **not** gated yet. It will be tracked under a future ticket.

## 5. Override Procedure (Exemption)

If a PR introduces a **temporary** coverage regression that the author and a maintainer agree is
acceptable (e.g. a large generated client landing ahead of its tests in a follow-up PR), the gate
may be bypassed by labelling the PR with **`coverage-exempt`**.

When the label is present:

- `scripts/check-coverage.mjs` exits 0 with a stderr warning.
- A reviewer must explicitly call out the exemption in their approval comment, including a link to
  the follow-up issue that will restore coverage.
- The exemption applies to that PR only. Re-introducing the same regression in a later PR requires
  a fresh label and a fresh justification.

Maintainers should periodically grep merged PRs for `label:coverage-exempt` to ensure follow-ups
land.

## 6. Local Reproduction

```bash
# Run the full backend test suite with coverage collection
dotnet test CoreAlign.sln -c Release --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Run the same gate the CI uses
node scripts/check-coverage.mjs --threshold 60 --path ./TestResults
```

## 7. Raising the Threshold

1. Verify three consecutive `main` builds at or above the new target.
2. Open a PR that:
   - Updates the `--threshold` value in `.github/workflows/ci.yml`.
   - Updates the "Current threshold" line in this document.
   - Lists the recent main builds that demonstrate sustained coverage.
3. Merge during a quiet window so in-flight feature branches can rebase against the new floor.

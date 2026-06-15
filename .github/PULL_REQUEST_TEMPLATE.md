<!--
  Thank you for the PR. Please fill in every section before requesting review.
  Sections with empty checklists will fail the review checklist gate.
-->

## Summary

<!-- One paragraph: what changes, why now. Link the ticket id (e.g. STOCK-014). -->

## Type

- [ ] feat: new user-visible capability
- [ ] fix: bug fix
- [ ] docs: documentation only
- [ ] refactor: behaviour-preserving cleanup
- [ ] test: tests only, no production code
- [ ] chore: build, CI, dependency, release housekeeping
- [ ] perf: performance improvement
- [ ] security: vulnerability or hardening change

## Acceptance Criteria

<!-- Copy from the ticket. Tick every item before requesting review. -->

- [ ] Acceptance criterion 1
- [ ] Acceptance criterion 2
- [ ] Acceptance criterion 3

## Risk and Rollback Plan

**Blast radius:** <!-- e.g. tenant-isolated, single module, infra-wide -->

**Rollback steps:**

1. <!-- e.g. revert PR + redeploy previous tag vX.Y.Z -->
2. <!-- e.g. run scripts/rollback-migration.sh if DB schema affected -->

**Feature flag / kill switch:** <!-- name of the flag or "n/a" -->

## Test Evidence

- [ ] `dotnet build CoreAlign.sln -c Release` -> 0 warnings, 0 errors
- [ ] `dotnet test CoreAlign.sln -c Release` -> >= 1016 Application + 55 Integration passing
- [ ] `npm run lint` -> exit 0
- [ ] `npm run typecheck` -> exit 0
- [ ] `npm run build` (tenant admin) -> exit 0
- [ ] `npm --workspace apps/customer-portal run build` -> exit 0
- [ ] `npm --workspace apps/b2b run build` -> exit 0
- [ ] Coverage gate passed (see `docs/coverage-policy.md`)

<!-- Paste relevant log snippets, screenshots, or attached artifacts below. -->

## Multi-Tenant Safety

- [ ] No raw SQL bypasses the EF global query filter.
- [ ] New aggregates inherit `TenantEntity`.
- [ ] `ITenantContext.RequireTenantId()` invoked at handler entry, not deep in repositories.

## Localisation

- [ ] All new user-visible strings added to `src/app/i18n/locales/en.json` and `tr.json`.
- [ ] All portal strings added to `apps/<portal>/src/app/locales/{en,tr}.json`.
- [ ] No hardcoded strings in `.tsx` outside of test fixtures.

## Changelog

- [ ] Added an entry under `## [Unreleased]` in `CHANGELOG.md` describing the user-visible change (or N/A for chore/refactor/test).

## Reviewer Notes

<!-- Optional: anything you want the reviewer to focus on, or known follow-ups. -->

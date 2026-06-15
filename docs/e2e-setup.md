# Playwright end-to-end test setup

This repository ships an `e2e/` Playwright harness that exercises three
single-page apps (admin, customer-portal, dealer/b2b) plus the backend stack.
The harness defaults to "smoke" mode: only the lightweight DOM checks run.
Full happy-path specs are gated behind the `E2E_LIVE_STACK=1` flag so CI is
green out of the box.

## Prerequisites

1. Install browsers (one-time):

   ```bash
   npm run e2e:install
   ```

2. Optional local stack ports (overridable):
   - `E2E_ADMIN_URL` (default `http://localhost:5173`)
   - `E2E_CUSTOMER_URL` (default `http://localhost:5174`)
   - `E2E_B2B_URL` (default `http://localhost:5175`)
   - `E2E_ADMIN_USER` `email:password` pair
   - `E2E_CUSTOMER_USER` `email:password` pair
   - `E2E_DEALER_USER` `email:password` pair
   - `E2E_LIVE_STACK=1` enables the full flows that depend on the
     backend (otherwise they are skipped).

## Running

```bash
# All projects (admin, customer-portal, b2b)
npm run e2e

# A single SPA
npm run e2e -- --project=admin
npm run e2e -- --project=customer-portal
npm run e2e -- --project=b2b
```

Reports land in `e2e/playwright-report/` and are uploaded by CI as an
artifact per matrix entry (see `.github/workflows/ci.yml` → job `e2e`).

## Adding tests

- Per-SPA specs go in `e2e/<spa>/*.spec.ts`.
- Reuse `loginAs` from `e2e/fixtures/loginFlow.ts` and add credentials to
  `e2e/fixtures/credentials.ts` if a new persona is needed.
- Use `test.skip(skipIfNoStack(), ...)` for any test that requires a live
  backend so the suite stays green in unit-only contexts.

## Known limits

- `@playwright/test` is listed in the root `package.json` but
  `npm install --save-dev @playwright/test` is required once on the
  developer machine (and on CI before the e2e job).
- The Iyzico payment flow test runs in mock mode unless `E2E_LIVE_STACK=1`
  is set with valid sandbox credentials.

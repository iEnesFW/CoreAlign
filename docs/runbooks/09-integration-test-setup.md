# 09 — Integration Test Setup

End-to-end multi-tenant safety net for the CoreAlign API plus React Testing
Library suites for the two B2B SPAs (customer-portal, b2b).

## Server: CoreAlign.Integration.Tests

Location: `server/tests/CoreAlign.Integration.Tests/`

The suite spins up the full ASP.NET Core pipeline through
`WebApplicationFactory<Program>` against an in-memory SQLite database
(`file:integration-<guid>?mode=memory&cache=shared`). A custom
`TestAuthenticationHandler` mints `ClaimsPrincipal`s straight from request
headers, so tests do not need to round-trip through the real JWT issuer.

Two tenants (`Tenant-A`, `Tenant-B`) are seeded by the fixture
`CoreAlignWebApiFactory` with the cast described in INFRA-017:

- 1 TenantAdmin user
- 1 Customer + 1 CustomerUser membership
- 1 DealerAccount + 1 DealerUser membership + 1 DealerCustomerLink
- 2 Products
- 1 Order
- 1 Invoice
- 1 Notification each for the customer-user and dealer-user

### Running

```bash
dotnet test server/tests/CoreAlign.Integration.Tests
```

The suite is also picked up by `dotnet test CoreAlign.sln`.

### What it asserts

- `CrossTenantIsolationTests` — Tenant-A admin cannot read or mutate any of
  Tenant-B's customers, orders, invoices, products, dealer-accounts, or
  customer-users via the admin API. List endpoints only return the caller's
  tenant rows.
- `PortalScopeIsolationTests` — Customer-A and Customer-B cannot read each
  other's invoices/orders via `customer-portal/*`; Dealer-A and Dealer-B
  cannot read each other's orders/invoices/customers via `dealer-portal/*`.
  Cross-persona attempts (customer hitting dealer endpoints and vice versa)
  return 403/404.

### Test header contract

`TestAuthenticationHandler` reads these headers per request:

| Header             | Description                                 |
| ------------------ | ------------------------------------------- |
| `X-Test-User-Id`   | The user GUID to mint claims for            |
| `X-Test-Tenant-Id` | The tenant GUID set on `tenant_id`          |
| `X-Test-Persona`   | `tenant`, `customer`, or `dealer`           |
| `X-Test-Roles`     | Comma-separated role names                  |
| `X-Test-Email`     | Optional; defaults to `<userId>@test.local` |

Use `HttpClient.AuthenticatedAs(tenant, persona)` from
`TestHttpClientExtensions` to apply these in one call.

### Adding a new isolation test

1. Add an endpoint to the relevant `*IsolationTests` class.
2. Call `AdminOfTenantA()` (or `_factory.CreateClient().AuthenticatedAs(...)`)
   and hit the route with a Tenant-B id from `_factory.TenantB`.
3. Assert one of `404 NotFound`, `403 Forbidden`, `400 BadRequest`, or
   `409 Conflict`. The shared `AssertDenied(...)` helper enforces this.
4. Avoid relying on `200 OK + empty list` as a pass — for endpoints that
   correctly return empty pages instead of 404, fall back to checking the
   response body does not leak the Tenant-B id.

## Frontend: customer-portal + b2b Vitest suites

Both SPAs share the same Vitest configuration shape (jsdom environment,
`./src/test/setup.ts`, RTL + jest-dom). Tests live under
`apps/<portal>/src/**/__tests__/*.test.tsx`.

### Running

```bash
npm --workspace apps/customer-portal test
npm --workspace apps/b2b test
```

### Coverage included

- Shared UI primitives: `Button`, `Input`, `Card`, `StatusBadge` variants.
- `authStore`: persistence into the per-portal localStorage namespace,
  expiry rejection on restore.
- Customer portal `NotificationBell`: unread badge thresholds, mark-all-read
  flow with mocked `portalNotificationsApi`.
- B2B `NewOrderForm`: customer select wiring, credit-panel rendering, hard
  credit limit disables submit.

### Adding a component test

- Mock outgoing HTTP via `vi.mock('@/features/.../api')` returning shaped
  data. Render with `QueryClientProvider` + `MemoryRouter` if the component
  uses TanStack Query or `react-router`.
- For language-independent assertions, prefer class-based checks
  (`badge.className.toContain('bg-rose-100')`) over text matches — i18n
  defaults to Turkish.
- Always wrap fetch-mocked components in `<QueryClientProvider>` with
  `defaultOptions: { queries: { retry: false } }` to keep tests deterministic.

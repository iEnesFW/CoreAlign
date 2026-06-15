# 5. JWT bearer tokens with `persona` claim

- Status: Accepted
- Date: 2026-03-13
- Deciders: Backend leads, Security
- Tags: auth, authorization, persona

## Context and Problem Statement

CoreAlign exposes the same backend to four very different audiences:

- **Customer** — end customers of a tenant business (e.g. a buyer in the customer portal).
- **Dealer** — B2B reseller persona with elevated catalog and pricing rights.
- **Tenant** — employees of the tenant business (the admin SPA).
- **PlatformAdmin** — CoreAlign operations staff with cross-tenant powers.

A single user may even hold multiple personas. We need an authorisation model that can express
persona-scoped policy without baking persona logic into every controller.

## Decision Drivers

- Stateless auth (no session table read on every request).
- One token per session, not one per persona.
- Policy decisions must be declarative and centrally registered.

## Considered Options

1. **JWT bearer** carrying `sub`, `tenant_id`, `persona`, and `role` claims; ASP.NET `AuthorizationPolicy` per persona.
2. Cookie-based session with server-side store.
3. JWT plus a per-request DB lookup of effective permissions.
4. OAuth scopes encoded as space-separated values in `scope` claim.

## Decision

We adopt **Option 1**:

- Access token (15 min TTL) and rotating refresh token (7 day TTL, single-use).
- Mandatory claims: `sub` (user id), `tenant_id`, `persona` (one of `Customer`, `Dealer`, `Tenant`, `PlatformAdmin`), `role` (free-form per persona).
- Persona policies (constants `PersonaPolicies.Customer`, `PersonaPolicies.Dealer`, `PersonaPolicies.Tenant`, `PersonaPolicies.PlatformAdmin`) are registered once in `Program.cs` and applied to controllers via `[Authorize(Policy = PersonaPolicies.Tenant)]`.
- All endpoints are `/api/v1` and require a persona policy attribute — there is no implicit allow-anonymous.

## Consequences

- Positive: stateless, scales horizontally, no session store on the hot path.
- Positive: persona logic centralised — controllers stay thin.
- Positive: composable — a token can hold multiple roles within one persona.
- Negative: short TTL means clients must implement refresh-token rotation. Mitigated by shared `safeRequest` HTTP helper in all three SPAs.
- Negative: a leaked token is valid until expiry. Mitigated by short TTL + ability to revoke refresh tokens server-side.

## Links

- Token issuance in `server/src/CoreAlign.Infrastructure/Services/JwtTokenService.cs` (read-only).
- Policy constants in `server/src/CoreAlign.API/Authorization/PersonaAuthorizationPolicies.cs` (read-only).
- Policy registration in `server/src/CoreAlign.API/Program.cs` (read-only).

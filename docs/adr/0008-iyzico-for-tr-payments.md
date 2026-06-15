# 8. Iyzico as the primary payment gateway for TR

- Status: Accepted
- Date: 2026-04-08
- Deciders: Backend leads, Product, Finance
- Tags: payments, integration, turkey

## Context and Problem Statement

Sprint 4 introduced paid invoices, returns, and credit notes. We needed a payment service provider
(PSP) that:

- Is fully compliant with BKM and BDDK regulations for Turkish card payments.
- Supports 3D-Secure mandatorily for TR-issued cards.
- Issues TRY-denominated settlement.
- Provides a sandbox for end-to-end test automation.

## Decision Drivers

- TR market fit: legal compliance, TRY-native settlement, popular local card brands.
- Developer experience and integration test support.
- Fee structure at our projected volume.
- Roadmap fit for future EUR / USD (international) payments.

## Considered Options

1. **Iyzico** — Turkish PSP, BKM/BDDK compliant, mature .NET SDK, sandbox available.
2. **Stripe** — best-in-class DX globally; TR support is limited and TRY settlement was not generally available at decision time.
3. **PayU TR** — local PSP, weaker .NET SDK.
4. **Param** — local PSP, integration is REST-only without an SDK.

## Decision

We adopt **Option 1**: Iyzico for the TR market.

- All payment flows go through an `IPaymentGateway` port. The Iyzico implementation lives in `CoreAlign.Infrastructure.Payments.Iyzico`.
- 3D-Secure is mandatory for all card payments; the callback handler is an idempotent endpoint keyed by the Iyzico `paymentId`.
- Card data never reaches our servers — we redirect to the Iyzico hosted page.
- A future ADR will document the EUR / USD provider (likely Stripe) for non-TR markets.

## Consequences

- Positive: regulatory compliance handled by the PSP.
- Positive: card data scope removed from PCI footprint (hosted page).
- Positive: settlement reports available via Iyzico dashboard for finance reconciliation.
- Negative: vendor lock-in for TR flows. Mitigated by the `IPaymentGateway` port and a swap-ready integration test suite (records & replays Iyzico sandbox responses).
- Negative: callback path is asynchronous; we rely on the outbox (ADR 0004) for reliable status propagation.

## Links

- Port: `server/src/CoreAlign.Application/Billing/Payments/IPaymentGateway.cs` (read-only).
- Implementation: `server/src/CoreAlign.Infrastructure/Payments/IyzicoPaymentGateway.cs` (read-only).
- Iyzico documentation: https://docs.iyzico.com/

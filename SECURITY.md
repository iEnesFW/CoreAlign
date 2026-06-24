# Security Policy

CoreAlign is a multi-tenant ERP that processes financial, personal, and
special-category data. We take the security of the platform and of our
customers' data seriously.

## Reporting a Vulnerability

If you believe you have found a security vulnerability, please report it
**privately**. Do **not** open a public GitHub issue, pull request, or
discussion for security problems.

- **Email:** security@artesis.com
- **Encryption:** PGP key available on request.
- **Response target:** we acknowledge reports within **3 business days** and aim
  to provide a remediation timeline within **10 business days**.

Please include, where possible:

- A description of the vulnerability and its impact.
- Steps to reproduce (proof-of-concept, affected endpoint/parameter).
- The affected component, version/commit, and environment.

We follow **coordinated disclosure**: we ask that you give us a reasonable
window to remediate before any public disclosure, and we will credit reporters
who wish to be acknowledged.

## Scope

In scope:

- The CoreAlign backend API (`server/`), the web SPAs (`src/`, `apps/`), and the
  mobile app (`mobile/`).
- Authentication, authorization, multi-tenant isolation, payment, and
  personal-data handling.

Out of scope:

- Findings that require physical access, social engineering, or a compromised
  end-user device.
- Denial-of-service via volumetric traffic.
- Reports from automated scanners without a demonstrated, exploitable impact.

## Supported Versions

Security fixes are applied to the `main` branch and the most recent released
version. Older versions may not receive security updates.

## Handling

Confirmed vulnerabilities are tracked privately, fixed on a priority basis,
and — where they affect customer data — handled in line with our incident- and
breach-response procedures (`docs/runbooks/04-incident-response.md`), including
the regulatory notification obligations under GDPR (Art. 33/34) and KVKK.

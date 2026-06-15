# Secrets & Environment Variables Inventory

This document catalogs every secret, credential, and environment-bound configuration value consumed by CoreAlign (API + 3 SPAs). Treat anything marked **Critical** or **High** as production-grade secrets and load them from a vault (Azure Key Vault, AWS SSM Parameter Store, HashiCorp Vault) — never commit them to git.

## API (server/src/CoreAlign.API)

| Key                                   | Type                 | Sensitivity | Set via      | Notes                                                                                                        |
| ------------------------------------- | -------------------- | ----------- | ------------ | ------------------------------------------------------------------------------------------------------------ |
| `ConnectionStrings:DefaultConnection` | string               | Critical    | env / vault  | Postgres connection string. Format: `Host=...;Port=5432;Database=corealign;Username=...;Password=...`        |
| `Database:Provider`                   | string               | Low         | env / config | `Postgres` (default) or `Sqlite` (dev fallback only)                                                         |
| `Database:AutoMigrate`                | bool                 | Low         | env / config | Keep `false` in prod; use explicit `--migrate` step from runbook                                             |
| `Jwt:SecretKey`                       | string (>= 64 chars) | Critical    | env / vault  | HMAC-SHA256 signing key. Rotate per `docs/runbooks/07-key-rotation.md`                                       |
| `Jwt:Issuer`                          | string               | Low         | config       | Default `CoreAlign.API`                                                                                      |
| `Jwt:Audience`                        | string               | Low         | config       | Default `CoreAlign.Client`                                                                                   |
| `Jwt:AccessTokenExpirationMinutes`    | int                  | Low         | config       | Default 15                                                                                                   |
| `Jwt:RefreshTokenExpirationDays`      | int                  | Low         | config       | Default 7                                                                                                    |
| `Auth:AutoConfirmEmail`               | bool                 | High        | config       | MUST be `false` outside Development. Startup throws if `true` and env != Development                         |
| `Cors:AllowedOrigins`                 | string[]             | Medium      | env / config | Set via indexed env vars: `Cors__AllowedOrigins__0=https://admin.example.com`, `Cors__AllowedOrigins__1=...` |
| `Iyzico:ApiKey`                       | string               | Critical    | env / vault  | From Iyzico merchant portal                                                                                  |
| `Iyzico:SecretKey`                    | string               | Critical    | env / vault  | From Iyzico merchant portal                                                                                  |
| `Iyzico:BaseUrl`                      | string               | Low         | config       | `https://api.iyzipay.com` prod, `https://sandbox-api.iyzipay.com` sandbox                                    |
| `Iyzico:CallbackBaseUrl`              | string               | Medium      | env / config | Public URL the API serves Iyzico callbacks from                                                              |
| `Iyzico:WebhookBaseUrl`               | string               | Medium      | env / config | Public URL Iyzico will POST webhooks to                                                                      |
| `Iyzico:DefaultLocale`                | string               | Low         | config       | `tr` or `en`                                                                                                 |
| `Iyzico:AllowInstallments`            | bool                 | Low         | config       | Toggle installment payment options                                                                           |
| `Iyzico:HttpTimeoutSeconds`           | int                  | Low         | config       | Default 30                                                                                                   |
| `Billing:DefaultGatewayName`          | string               | Low         | config       | `iyzico` in prod, `mock` only in dev                                                                         |
| `Billing:EnableMockGateway`           | bool                 | High        | config       | MUST be `false` in prod                                                                                      |
| `Email:Smtp:Host`                     | string               | Medium      | env / config | SMTP relay host                                                                                              |
| `Email:Smtp:Port`                     | int                  | Low         | env / config | 587 (TLS) or 465 (SSL)                                                                                       |
| `Email:Smtp:Username`                 | string               | Medium      | env / vault  | SMTP auth user                                                                                               |
| `Email:Smtp:Password`                 | string               | Critical    | env / vault  | SMTP auth password / app password                                                                            |
| `Email:Smtp:UseSsl`                   | bool                 | Low         | env / config | `true` for production                                                                                        |
| `Email:Smtp:FromAddress`              | string               | Low         | env / config | `noreply@yourdomain.com`                                                                                     |
| `Email:Smtp:FromName`                 | string               | Low         | env / config | Sender display name                                                                                          |
| `Sentry:Dsn`                          | string               | Medium      | env / vault  | (Future) — error tracker DSN                                                                                 |
| `Sentry:Environment`                  | string               | Low         | env / config | `production`, `staging`, `development`                                                                       |
| `Sentry:TracesSampleRate`             | double               | Low         | env / config | 0.0 – 1.0                                                                                                    |
| `Configuration:VaultProvider`         | string               | Low         | env / config | `AzureKeyVault`, `AwsSsm`, or empty                                                                          |
| `ASPNETCORE_ENVIRONMENT`              | string               | Low         | env          | `Production`, `Staging`, `Development`                                                                       |
| `ASPNETCORE_URLS`                     | string               | Low         | env          | Default `http://+:8080` in container                                                                         |

### Required-for-startup in Production

The following must be present and non-empty when `ASPNETCORE_ENVIRONMENT=Production`:

- `ConnectionStrings:DefaultConnection`
- `Jwt:SecretKey` (>= 64 chars)
- At least one `Cors:AllowedOrigins[*]`
- `Iyzico:ApiKey` + `Iyzico:SecretKey` (if billing enabled)

## SPAs (tenant-admin + customer-portal + b2b)

Frontend env vars are inlined at build time by Vite. Every SPA has its own `.env.example`.

| Key                       | Type   | Sensitivity | Set via      | Notes                                                       |
| ------------------------- | ------ | ----------- | ------------ | ----------------------------------------------------------- |
| `VITE_API_URL`            | string | Low         | per-SPA .env | Public base URL of the API (e.g. `https://api.example.com`) |
| `VITE_RECAPTCHA_SITE_KEY` | string | Low         | per-SPA .env | reCAPTCHA v3 site key (public, but per-environment)         |

## Setting secrets

### Local development

Use [`dotnet user-secrets`](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — the API project is wired with `UserSecretsId=corealign-api-dev`:

```bash
cd server/src/CoreAlign.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=corealign;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)"
dotnet user-secrets set "Iyzico:ApiKey" "sandbox-..."
dotnet user-secrets set "Iyzico:SecretKey" "sandbox-..."
```

SPAs read `.env` (root) or `apps/*/.env` — copy from the corresponding `.env.example`:

```bash
cp .env.example .env
cp apps/customer-portal/.env.example apps/customer-portal/.env
cp apps/b2b/.env.example apps/b2b/.env
```

### Production

Inject every secret via environment variables (Kubernetes secrets, Docker secrets, ECS task-def, App Service settings). The .NET configuration system maps `__` (double underscore) to `:` (colon). Examples:

```bash
ConnectionStrings__DefaultConnection='Host=...;...'
Jwt__SecretKey='...'
Cors__AllowedOrigins__0='https://admin.example.com'
Cors__AllowedOrigins__1='https://customers.example.com'
Iyzico__ApiKey='...'
Iyzico__SecretKey='...'
Email__Smtp__Password='...'
```

Set `Configuration:VaultProvider=AzureKeyVault` (and the corresponding KV URI) or `Configuration:VaultProvider=AwsSsm` (and the parameter prefix) to pull from a managed vault at startup — see `docs/runbooks/07-key-rotation.md` for the supported secret names.

### CI / GitHub Actions

Store all production secrets in GitHub Environments (`production`, `staging`) and reference them in workflows with `${{ secrets.SECRET_NAME }}`. Never echo a secret to logs — GitHub auto-masks, but commands like `set -x` can leak parts of them.

## Rotation policy

| Secret            | Rotation cadence                | Owner              |
| ----------------- | ------------------------------- | ------------------ |
| `Jwt:SecretKey`   | 90 days                         | Platform team      |
| Iyzico keys       | On compromise / annual review   | Finance + Platform |
| SMTP password     | 180 days                        | Platform team      |
| Postgres password | 180 days                        | DBA                |
| Sentry DSN        | Per environment, on team change | Observability      |

## What is NOT a secret

These are safe to commit and live in `appsettings.*.json` or `vite.config.ts`:

- API issuer / audience names
- Token lifetimes (minutes/days)
- Sentry sample rates
- Locale defaults
- Rate-limit numerics
- Public reCAPTCHA site key (though we still gitignore `.env` to avoid mixing)

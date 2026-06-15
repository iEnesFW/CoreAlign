# 10. Demo Data Seeding

`DemoDataSeeder` is a one-shot `BackgroundService` that bootstraps a fully
populated demo tenant (admin user, FX rates, products, customers, vendors,
purchasing + sales chains, B2B identity) the first time it sees an empty
database. It is **destructive in intent** — it creates the canonical
`admin@demo.local` user with a well-known password — and is therefore gated.

## Activation Flags

The seeder runs only if **both** of the following are true:

1. **The host is NOT Production.** `IHostEnvironment.IsProduction()` returning
   true unconditionally blocks the seeder.
2. **At least one of these flags is set:**
   | Source | Value | Notes |
   | ---------------------- | ----------------- | --------------------------------------------------------------- |
   | Env var `DEMO_DATA` | `true` or `1` | Case-insensitive (`true` / `TRUE` / `True`). |
   | Config `DemoData:Enabled` | `true` | Read from `appsettings.{Env}.json` or any IConfiguration source.|

When neither flag is set, the seeder boots, logs a single line, and exits.

## Production Safety

If **any** activation flag is set while the host environment is
`Production`, the seeder throws
`InvalidOperationException` **before** `WebApplication.Build()` returns,
crashing the deployment immediately. This is intentional: a silent skip in
prod could mask a misconfigured stage that we expected to seed.

## Local Development

`appsettings.Development.json` ships with `DemoData:Enabled` unset to keep
fresh clones quiet. Enable seeding when you actually want demo data:

```powershell
$env:DEMO_DATA = "true"; dotnet run --project server/src/CoreAlign.API
```

Or persist it via your local user secrets:

```bash
dotnet user-secrets set "DemoData:Enabled" "true" --project server/src/CoreAlign.API
```

The seeder is idempotent: once `admin@demo.local` exists it short-circuits
and does nothing on subsequent boots.

## Disabling After Seeding

Once your local DB has the demo tenant you can leave `DEMO_DATA` set — the
idempotency check makes additional runs cheap (one user-lookup query). To
remove the demo entirely, drop the database and restart with the flag
unset.

## Staging / CI

CI never sets `DEMO_DATA` and runs against ephemeral Postgres containers.
Integration tests build the host with a dedicated factory that does not
register `DemoDataSeeder`, so the safety gate does not interfere with the
test suite.

## Related Files

- `server/src/CoreAlign.API/HostedServices/DemoDataSeeder.cs`
- `server/src/CoreAlign.API/Program.cs` (registration + startup gate)

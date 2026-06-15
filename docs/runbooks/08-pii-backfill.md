# 08 — PII Column Encryption Backfill

## Scope

Phase31PiiEncryption widens the following columns to `varchar(512)` to hold ciphertext, but does NOT auto-encrypt rows written before the migration. This runbook covers backfilling existing plaintext into ciphertext using the configured DataProtection provider.

Columns covered:

- `customers.national_id`
- `vendor_bank_accounts.iban`
- `users.phone_number`

## When to run

Run once, after Phase31 ships to production, during a planned maintenance window. Concurrent writes should be paused or the rows touched by application traffic should be re-encrypted afterward (the operation is idempotent: re-encrypting an already-encrypted value will fail on Unprotect — see "Idempotency" below).

## Prerequisites

- DataProtection keys path configured (`DataProtection:KeysPath`) and stable. Rotating keys mid-backfill leaves the row undecryptable.
- DB backup taken (`pg_dump`) within the last hour.
- `corealign` service stopped or in read-only mode for the affected tables.

## CLI

A `--backfill-pii` flag will be added to the API host:

```
dotnet run --project server/src/CoreAlign.API -- --backfill-pii
```

This iterates rows for each target table, reads the plaintext, calls `IDataProtector.Protect(...)` with purpose `corealign.pii.v1`, and writes the ciphertext back via a `UPDATE` statement that bypasses the EF value converter (executed as raw SQL to avoid double-encryption).

Algorithm (per table):

```
for row in SELECT id, <column> FROM <table> WHERE <column> IS NOT NULL AND <column> NOT LIKE 'CfDJ%':
    encrypted = protector.Protect(row.column)
    UPDATE <table> SET <column> = encrypted WHERE id = row.id
```

The `CfDJ%` filter is the ASP.NET DataProtection ciphertext prefix and acts as a poor-man's "already encrypted" check.

## Idempotency

The backfill is idempotent because:

1. The `WHERE <column> NOT LIKE 'CfDJ%'` predicate excludes rows already in ciphertext.
2. Even if rerun, only plaintext rows are touched.

If an attacker can write plaintext that happens to start with `CfDJ`, the heuristic fails. For deterministic safety, persist a backfill marker in `tenant_settings` keyed `Pii:BackfillCompletedAtUtc` after the first successful run and exit early on subsequent invocations.

## Rollback

If decryption begins to fail (e.g. wrong key environment), restore from the pre-backfill `pg_dump`. There is no SQL-only reverse — only the application can decrypt rows.

## Verification

After backfill:

1. SELECT 5 random rows per table and confirm the column starts with `CfDJ`.
2. Smoke-test the application end-to-end: load a customer profile, vendor bank account list, user profile — observe values render as plaintext (decryption succeeded).
3. Run the test suite: `dotnet test CoreAlign.sln --filter Category=Smoke`.

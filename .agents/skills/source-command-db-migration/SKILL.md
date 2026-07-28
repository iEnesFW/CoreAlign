---
name: source-command-db-migration
description: Create and verify CoreAlign EF Core migrations using the repository's future Phase ID, idempotency, snapshot, same-turn apply, and tabula-rasa rules. Use when the user invokes db-migration or a CoreAlign change requires a migration, schema change, index, constraint, or EF model snapshot update.
---

# CoreAlign DB Migration

1. Read `AGENTS.md` §4.2, §4.12, §12 and §17 plus `docs/INVARIANTS.md` before editing.
2. Generate with a build so EF uses the current assembly:

```powershell
dotnet ef migrations add <Name> -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API -o Persistence/Migrations
```

3. Rename the generated ID after the latest existing future `PhaseNN` migration. Update the migration filename, `.Designer.cs` filename, class name, and `[Migration("...")]` value consistently. Preserve the generated snapshot.
4. Use `decimal(18,4)` for money, `timestamptz` for time, tenant-leading unique indexes, required foreign keys and indexes, and relevant CHECK constraints. Make destructive or raw steps idempotent.
5. Check `has-pending-model-changes`. If another active change owns the snapshot, follow AGENTS.md §12.9: write the required idempotent migration without touching the snapshot and report the blocker.
6. For `IGlobalReadable` entities, verify the tenant foreign-key exclusion in §4.12.
7. Apply the migration in the same task with `dotnet ef database update` unless the target environment or user authorization makes this unsafe.
8. For tabula-rasa validation, request explicit approval before dropping any database. Recreate and apply all migrations, then fix ordering or already-exists failures.
9. Ensure raw-SQL-only indexes are represented in the model or registered in `docs/RAW_SQL_INDEX_REGISTRY.md` and `docs/INVARIANTS.md`.
10. Finish with `$source-command-pre-ship` and report each migration validation gate as PASS or FAIL.

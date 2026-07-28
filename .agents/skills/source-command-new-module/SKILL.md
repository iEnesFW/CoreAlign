---
name: source-command-new-module
description: Build a new CoreAlign module with the repository's FSD frontend and Clean Architecture CQRS backend patterns. Use when the user invokes new-module or requests a new bounded module, feature area, entity-backed workflow, admin page group, portal module, or mobile module in CoreAlign.
---

# CoreAlign New Module

1. Read `AGENTS.md`, `docs/INVARIANTS.md`, the matching §0.1 module row, and the closest existing module. Follow the established pattern over inventing a new one.
2. Select the correct frontend surface: admin `src/`, customer portal `apps/customer-portal`, B2B portal `apps/b2b`, or field mobile `mobile/`. Do not import across surfaces.
3. Derive new tenant-owned data entities from `TenantEntity`. Apply the §4.6 concurrency mechanism when records can race.
4. Add EF configuration with the repository's snake_case conventions, foreign keys, indexes, `decimal(18,4)` money, and `timestamptz` time.
5. Use `$source-command-db-migration` for any schema change.
6. Build `Application/<Module>/{Commands,Queries,Validators,DTOs,Handlers}` with repository data access, no N+1 queries, and a slim controller.
7. Build frontend `features/<x>/{api,hooks,model,ui}` plus the correct page layer. Use semantic design tokens, responsive and dark states, and synchronized Turkish and English translations.
8. For money or stock mutation, add durable idempotency, a transaction boundary, audit, concurrency protection, and the required domain invariants.
9. Run `$source-command-pre-ship` and perform the AGENTS.md §15 invariant review before handoff.

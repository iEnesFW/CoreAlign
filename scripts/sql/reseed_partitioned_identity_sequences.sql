-- ============================================================================
-- reseed_partitioned_identity_sequences.sql
--
-- OPTIONAL hygiene. Not required to stop the login-time DbUpdateConcurrencyException
-- (that is fixed in code: MaintenanceDataAccess now anonymizes via a set-based bulk
-- UPDATE instead of a per-row tracked SaveChanges). This script cleans up the root cause
-- of the messy state it exposed.
--
-- Phase86 rebuilt the high-growth leaf tables as RANGE-partitioned via
--   CREATE TABLE <t> (LIKE <t>_pre_part INCLUDING DEFAULTS INCLUDING IDENTITY) PARTITION BY ...
-- INCLUDING IDENTITY creates a FRESH identity sequence for the new table starting at 1.
-- The following INSERT ... SELECT * supplied explicit id values and therefore did NOT
-- advance that sequence. As a result, on any such table whose id is a bigint identity
-- (today: login_audit_logs), new inserts reuse low id values. This is tolerated at runtime
-- only because the partitioned PK is composite (id, <ts_col>), so a reused id with a fresh
-- timestamp does not collide -- but it leaves duplicate id values in the table.
--
-- This script advances each identity sequence past MAX(id) so future inserts get unique
-- ids. It is IDEMPOTENT (setval to the current max is a no-op when already ahead) and only
-- touches tables that actually have an identity sequence (Guid-PK tables are skipped).
--
-- Apply against the target database, e.g.:
--   psql "<connection string>" -f scripts/sql/reseed_partitioned_identity_sequences.sql
-- ============================================================================

DO $$
DECLARE
    t       text;
    seq     text;
    max_id  bigint;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'login_audit_logs',
        'activity_logs',
        'outbox_messages',
        'stock_movements',
        'customer_transactions',
        'error_logs',
        'customer_ledger_entries',
        'vendor_ledger_entries',
        'stock_transactions'
    ]
    LOOP
        seq := pg_get_serial_sequence(t, 'id');
        IF seq IS NULL THEN
            -- Guid PK or no identity column: nothing to reseed.
            CONTINUE;
        END IF;

        EXECUTE format('SELECT COALESCE(MAX(id), 0) FROM %I', t) INTO max_id;

        -- is_called = (max_id > 0): non-empty -> nextval returns max_id + 1;
        --                           empty     -> nextval returns 1.
        PERFORM setval(seq, GREATEST(max_id, 1), max_id > 0);

        RAISE NOTICE 'reseeded %.id sequence % (max id = %)', t, seq, max_id;
    END LOOP;
END $$;

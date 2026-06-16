using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase89PartitionFnAndScaleIndexes : Migration
    {
        // Raw-SQL, additive. (1) Hardens corealign_ensure_future_partitions: UTC-pinned
        // boundaries (offline +03 hosts vs UTC prod no longer open a month-boundary gap)
        // + per-month BEGIN/EXCEPTION isolation (a conflicting month no longer aborts all
        // later months — the self-perpetuating rollover cliff). (2) Adds the 3DS-callback
        // global-lookup index and the service_tickets trailing-sort indexes that Phase88
        // missed. Snapshot unchanged (no model change).
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION corealign_ensure_future_partitions(p_table text, p_ts_col text, p_months_ahead int)
RETURNS void AS $fn$
DECLARE
    m date;
    i int;
    part_name text;
    lo timestamptz;
    hi timestamptz;
BEGIN
    -- Deterministic UTC boundaries regardless of session/host TimeZone.
    SET LOCAL TimeZone = 'UTC';
    m := date_trunc('month', now())::date;

    FOR i IN 0..p_months_ahead LOOP
        part_name := p_table || '_p' || to_char(m, 'YYYYMM');
        lo := m::timestamptz;
        hi := (m + interval '1 month')::date::timestamptz;

        IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = part_name) THEN
            -- Per-month isolation: a failure on one month (e.g. rows already in the
            -- DEFAULT partition for this range) must NOT abort the remaining months.
            BEGIN
                EXECUTE format('CREATE TABLE %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
                    part_name, p_table, lo, hi);
            EXCEPTION WHEN others THEN
                RAISE WARNING 'corealign_ensure_future_partitions: skipped % for %: %', part_name, p_table, SQLERRM;
            END;
        END IF;

        m := (m + interval '1 month')::date;
    END LOOP;
END;
$fn$ LANGUAGE plpgsql;

CREATE INDEX IF NOT EXISTS ix_payment_transactions_provider_external
  ON payment_transactions (provider_name, external_transaction_id)
  WHERE is_deleted = false AND external_transaction_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_service_tickets_tenant_status_reported
  ON service_tickets (tenant_id, status, reported_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_service_tickets_tenant_reported
  ON service_tickets (tenant_id, reported_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_service_tickets_tenant_customer_reported
  ON service_tickets (tenant_id, customer_id, reported_at_utc DESC, id DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_payment_transactions_provider_external;
DROP INDEX IF EXISTS ix_service_tickets_tenant_status_reported;
DROP INDEX IF EXISTS ix_service_tickets_tenant_reported;
DROP INDEX IF EXISTS ix_service_tickets_tenant_customer_reported;

-- Restore the pre-Phase89 function body (name-only guard, no UTC pin, no isolation).
CREATE OR REPLACE FUNCTION corealign_ensure_future_partitions(p_table text, p_ts_col text, p_months_ahead int)
RETURNS void AS $fn$
DECLARE
    m date := date_trunc('month', now())::date;
    i int;
    part_name text;
BEGIN
    FOR i IN 0..p_months_ahead LOOP
        part_name := p_table || '_p' || to_char(m, 'YYYYMM');
        IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = part_name) THEN
            EXECUTE format('CREATE TABLE %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
                part_name, p_table, m, (m + interval '1 month')::date);
        END IF;
        m := (m + interval '1 month')::date;
    END LOOP;
END;
$fn$ LANGUAGE plpgsql;");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase95PartitionLedgerAndErrorLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION corealign_partition_leaf_table_v2(p_table text, p_ts_col text, p_start date, p_months int)
RETURNS void AS $fn$
DECLARE
    src text := p_table || '_pre_part';
    m date := p_start; i int; part_name text;
    idx_def text; idx_defs text[] := '{}';
    fk_def text; fk_defs text[] := '{}';
    has_tenant_fk boolean;
BEGIN
    IF (SELECT relkind FROM pg_class WHERE relname = p_table) = 'p' THEN
        RETURN;
    END IF;

    has_tenant_fk := EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class cl ON cl.oid = con.conrelid
        JOIN pg_class ref ON ref.oid = con.confrelid
        WHERE con.contype = 'f' AND cl.relname = p_table AND ref.relname = 'tenants');

    FOR idx_def IN
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = p_table AND indexdef NOT LIKE 'CREATE UNIQUE%'
    LOOP
        idx_defs := array_append(idx_defs, idx_def);
    END LOOP;

    FOR fk_def IN
        SELECT 'ALTER TABLE public.' || quote_ident(p_table) || ' ADD CONSTRAINT '
               || quote_ident(conname) || ' ' || pg_get_constraintdef(oid)
        FROM pg_constraint WHERE conrelid = ('public.' || p_table)::regclass AND contype = 'f'
    LOOP
        fk_defs := array_append(fk_defs, fk_def);
    END LOOP;

    EXECUTE format('ALTER TABLE %I RENAME TO %I', p_table, src);
    EXECUTE format('CREATE TABLE %I (LIKE %I INCLUDING DEFAULTS INCLUDING IDENTITY) PARTITION BY RANGE (%I)', p_table, src, p_ts_col);
    EXECUTE format('ALTER TABLE %I ADD PRIMARY KEY (id, %I)', p_table, p_ts_col);

    FOR i IN 0..p_months-1 LOOP
        part_name := p_table || '_p' || to_char(m, 'YYYYMM');
        EXECUTE format('CREATE TABLE %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
            part_name, p_table, m, (m + interval '1 month')::date);
        m := (m + interval '1 month')::date;
    END LOOP;
    EXECUTE format('CREATE TABLE %I PARTITION OF %I DEFAULT', p_table || '_pdefault', p_table);

    EXECUTE format('INSERT INTO %I SELECT * FROM %I', p_table, src);
    EXECUTE format('DROP TABLE %I', src);

    FOREACH idx_def IN ARRAY idx_defs LOOP EXECUTE idx_def; END LOOP;
    FOREACH fk_def IN ARRAY fk_defs LOOP EXECUTE fk_def; END LOOP;

    EXECUTE format('CREATE INDEX %I ON %I USING brin (%I)', 'brin_' || p_table || '_' || p_ts_col, p_table, p_ts_col);

    IF has_tenant_fk THEN
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', p_table);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', p_table);
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I', p_table);
        EXECUTE format($pol$CREATE POLICY tenant_isolation ON %I
            USING (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1')
            WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1')$pol$, p_table);
    END IF;
END;
$fn$ LANGUAGE plpgsql;

SELECT corealign_partition_leaf_table_v2('error_logs', 'occurred_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table_v2('customer_ledger_entries', 'occurred_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table_v2('vendor_ledger_entries', 'occurred_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table_v2('stock_transactions', 'occurred_at_utc', DATE '2026-01-01', 24);

SELECT corealign_ensure_future_partitions('error_logs', 'occurred_at_utc', 6);
SELECT corealign_ensure_future_partitions('customer_ledger_entries', 'occurred_at_utc', 6);
SELECT corealign_ensure_future_partitions('vendor_ledger_entries', 'occurred_at_utc', 6);
SELECT corealign_ensure_future_partitions('stock_transactions', 'occurred_at_utc', 6);

DROP FUNCTION corealign_partition_leaf_table_v2(text, text, date, int);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION corealign_unpartition_table_v2(p_table text)
RETURNS void AS $fn$
DECLARE
    src text := p_table || '_part';
    idx_def text; idx_defs text[] := '{}';
    fk_def text; fk_defs text[] := '{}';
    has_tenant_fk boolean;
BEGIN
    IF (SELECT relkind FROM pg_class WHERE relname = p_table) <> 'p' THEN
        RETURN;
    END IF;

    has_tenant_fk := EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class cl ON cl.oid = con.conrelid
        JOIN pg_class ref ON ref.oid = con.confrelid
        WHERE con.contype = 'f' AND cl.relname = p_table AND ref.relname = 'tenants');

    FOR idx_def IN
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = p_table AND indexdef NOT LIKE 'CREATE UNIQUE%' AND indexdef NOT LIKE '%USING brin%'
    LOOP
        idx_defs := array_append(idx_defs, idx_def);
    END LOOP;

    FOR fk_def IN
        SELECT 'ALTER TABLE public.' || quote_ident(p_table) || ' ADD CONSTRAINT '
               || quote_ident(conname) || ' ' || pg_get_constraintdef(oid)
        FROM pg_constraint WHERE conrelid = ('public.' || p_table)::regclass AND contype = 'f'
    LOOP
        fk_defs := array_append(fk_defs, fk_def);
    END LOOP;

    EXECUTE format('ALTER TABLE %I RENAME TO %I', p_table, src);
    EXECUTE format('CREATE TABLE %I (LIKE %I INCLUDING DEFAULTS INCLUDING IDENTITY)', p_table, src);
    EXECUTE format('ALTER TABLE %I ADD PRIMARY KEY (id)', p_table);
    EXECUTE format('INSERT INTO %I SELECT * FROM %I', p_table, src);
    EXECUTE format('DROP TABLE %I CASCADE', src);

    FOREACH idx_def IN ARRAY idx_defs LOOP EXECUTE idx_def; END LOOP;
    FOREACH fk_def IN ARRAY fk_defs LOOP EXECUTE fk_def; END LOOP;

    IF has_tenant_fk THEN
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', p_table);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', p_table);
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I', p_table);
        EXECUTE format($pol$CREATE POLICY tenant_isolation ON %I
            USING (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1')
            WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1')$pol$, p_table);
    END IF;
END;
$fn$ LANGUAGE plpgsql;

SELECT corealign_unpartition_table_v2('error_logs');
SELECT corealign_unpartition_table_v2('customer_ledger_entries');
SELECT corealign_unpartition_table_v2('vendor_ledger_entries');
SELECT corealign_unpartition_table_v2('stock_transactions');

DROP FUNCTION corealign_unpartition_table_v2(text);");
        }
    }
}

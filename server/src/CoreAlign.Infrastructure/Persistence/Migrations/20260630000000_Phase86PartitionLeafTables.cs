using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase86PartitionLeafTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
$fn$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION corealign_partition_leaf_table(p_table text, p_ts_col text, p_start date, p_months int)
RETURNS void AS $fn$
DECLARE
    src text := p_table || '_pre_part';
    m date := p_start;
    i int;
    part_name text;
    idx_def text;
    idx_defs text[] := '{}';
    has_tenant boolean;
BEGIN
    has_tenant := EXISTS (SELECT 1 FROM information_schema.columns
                          WHERE table_schema = 'public' AND table_name = p_table AND column_name = 'tenant_id');

    EXECUTE format('ALTER TABLE %I RENAME TO %I', p_table, src);

    FOR idx_def IN
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = src AND indexdef NOT LIKE 'CREATE UNIQUE%'
    LOOP
        idx_defs := array_append(idx_defs, replace(idx_def, 'public.' || src, 'public.' || p_table));
    END LOOP;

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

    FOREACH idx_def IN ARRAY idx_defs LOOP
        EXECUTE idx_def;
    END LOOP;

    EXECUTE format('CREATE INDEX %I ON %I USING brin (%I)', 'brin_' || p_table || '_' || p_ts_col, p_table, p_ts_col);

    IF has_tenant THEN
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', p_table);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', p_table);
        EXECUTE format($pol$CREATE POLICY tenant_isolation ON %I USING (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1') WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1')$pol$, p_table);
        EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT',
            p_table, 'fk_' || p_table || '_tenants_tenant_id');
    END IF;
END;
$fn$ LANGUAGE plpgsql;

SELECT corealign_partition_leaf_table('activity_logs', 'created_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table('login_audit_logs', 'attempted_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table('outbox_messages', 'created_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table('notification_messages', 'created_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table('stock_movements', 'occurred_at_utc', DATE '2026-01-01', 24);
SELECT corealign_partition_leaf_table('customer_transactions', 'occurred_at_utc', DATE '2026-01-01', 24);

DROP FUNCTION corealign_partition_leaf_table(text, text, date, int);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION corealign_unpartition_table(p_table text)
RETURNS void AS $fn$
DECLARE
    src text := p_table || '_part';
    idx_def text;
    idx_defs text[] := '{}';
    has_tenant boolean;
BEGIN
    has_tenant := EXISTS (SELECT 1 FROM information_schema.columns
                          WHERE table_schema = 'public' AND table_name = p_table AND column_name = 'tenant_id');
    EXECUTE format('ALTER TABLE %I RENAME TO %I', p_table, src);
    FOR idx_def IN
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = src AND indexdef NOT LIKE 'CREATE UNIQUE%'
    LOOP
        idx_defs := array_append(idx_defs, replace(idx_def, 'public.' || src, 'public.' || p_table));
    END LOOP;
    EXECUTE format('CREATE TABLE %I (LIKE %I INCLUDING DEFAULTS INCLUDING IDENTITY)', p_table, src);
    EXECUTE format('ALTER TABLE %I ADD PRIMARY KEY (id)', p_table);
    EXECUTE format('INSERT INTO %I SELECT * FROM %I', p_table, src);
    EXECUTE format('DROP TABLE %I CASCADE', src);
    FOREACH idx_def IN ARRAY idx_defs LOOP
        EXECUTE idx_def;
    END LOOP;
    IF has_tenant THEN
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', p_table);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', p_table);
        EXECUTE format($pol$CREATE POLICY tenant_isolation ON %I USING (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1') WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid OR current_setting('app.rls_bypass', true) = '1')$pol$, p_table);
        EXECUTE format('ALTER TABLE %I ADD CONSTRAINT %I FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT',
            p_table, 'fk_' || p_table || '_tenants_tenant_id');
    END IF;
END;
$fn$ LANGUAGE plpgsql;

SELECT corealign_unpartition_table('activity_logs');
SELECT corealign_unpartition_table('login_audit_logs');
SELECT corealign_unpartition_table('outbox_messages');
SELECT corealign_unpartition_table('notification_messages');
SELECT corealign_unpartition_table('stock_movements');
SELECT corealign_unpartition_table('customer_transactions');

DROP FUNCTION corealign_unpartition_table(text);
DROP FUNCTION IF EXISTS corealign_ensure_future_partitions(text, text, int);");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase87NotificationIdempotencyFix : Migration
    {
        // WHY: Phase86 partitioned notification_messages but its (tenant_id, idempotency_hash)
        // unique index can't span partitions, so it was silently dropped -> idempotency/dedup lost.
        // A partitioned unique MUST include the partition key, which defeats idempotency; so we
        // un-partition this table (like entity_audit_logs) and restore the strict unique.
        protected override void Up(MigrationBuilder migrationBuilder)
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

SELECT corealign_unpartition_table('notification_messages');
DROP FUNCTION corealign_unpartition_table(text);

CREATE UNIQUE INDEX IF NOT EXISTS ux_notification_messages_tenant_idempotency
    ON notification_messages (tenant_id, idempotency_hash) WHERE idempotency_hash <> '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ux_notification_messages_tenant_idempotency;");
        }
    }
}

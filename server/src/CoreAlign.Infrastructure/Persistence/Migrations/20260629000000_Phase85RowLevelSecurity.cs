using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase85RowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $outer$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'corealign_app') THEN
        CREATE ROLE corealign_app LOGIN;
    END IF;
END
$outer$;

GRANT USAGE ON SCHEMA public TO corealign_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO corealign_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO corealign_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO corealign_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO corealign_app;

DO $outer$
DECLARE t text;
BEGIN
    FOR t IN
        SELECT DISTINCT cl.relname
        FROM pg_constraint con
        JOIN pg_class cl ON cl.oid = con.conrelid
        JOIN pg_class ref ON ref.oid = con.confrelid
        JOIN pg_namespace ns ON ns.oid = cl.relnamespace
        WHERE con.contype = 'f'
          AND ref.relname = 'tenants'
          AND ns.nspname = 'public'
          AND cl.relname <> 'users'
    LOOP
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t);
        EXECUTE format('ALTER TABLE public.%I FORCE ROW LEVEL SECURITY', t);
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON public.%I', t);
        EXECUTE format(
            $pol$CREATE POLICY tenant_isolation ON public.%I
                 USING (tenant_id = current_setting('app.tenant_id', true)::uuid
                        OR current_setting('app.rls_bypass', true) = '1')
                 WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid
                        OR current_setting('app.rls_bypass', true) = '1')$pol$, t);
    END LOOP;
END
$outer$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $outer$
DECLARE t text;
BEGIN
    FOR t IN
        SELECT DISTINCT cl.relname
        FROM pg_constraint con
        JOIN pg_class cl ON cl.oid = con.conrelid
        JOIN pg_class ref ON ref.oid = con.confrelid
        JOIN pg_namespace ns ON ns.oid = cl.relnamespace
        WHERE con.contype = 'f'
          AND ref.relname = 'tenants'
          AND ns.nspname = 'public'
          AND cl.relname <> 'users'
    LOOP
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON public.%I', t);
        EXECUTE format('ALTER TABLE public.%I NO FORCE ROW LEVEL SECURITY', t);
        EXECUTE format('ALTER TABLE public.%I DISABLE ROW LEVEL SECURITY', t);
    END LOOP;
END
$outer$;");
        }
    }
}

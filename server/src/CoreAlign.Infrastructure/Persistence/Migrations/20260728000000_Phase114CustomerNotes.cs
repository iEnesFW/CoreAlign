using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase114CustomerNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS customer_notes (
                    id uuid NOT NULL,
                    customer_id uuid NOT NULL,
                    created_by_user_id uuid NOT NULL,
                    body text NOT NULL,
                    tenant_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_customer_notes PRIMARY KEY (id),
                    CONSTRAINT fk_customer_notes_customers_customer_id FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE,
                    CONSTRAINT fk_customer_notes_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_customer_notes_customer_id ON customer_notes (customer_id);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_customer_notes_tenant_id_customer_id_created_at_utc ON customer_notes (tenant_id, customer_id, created_at_utc DESC);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS customer_notes;");
        }
    }
}

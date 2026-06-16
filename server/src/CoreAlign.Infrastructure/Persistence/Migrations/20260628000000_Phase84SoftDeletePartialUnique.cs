using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase84SoftDeletePartialUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ux_warranty_contracts_tenant_number;
DROP INDEX IF EXISTS ix_warranty_contracts_tenant_id_number;
CREATE UNIQUE INDEX IF NOT EXISTS ux_warranty_contracts_tenant_number
    ON warranty_contracts (tenant_id, number) WHERE is_deleted = false;

DROP INDEX IF EXISTS ix_purchase_requisitions_tenant_id_number;
DROP INDEX IF EXISTS ux_purchase_requisitions_tenant_number;
CREATE UNIQUE INDEX IF NOT EXISTS ix_purchase_requisitions_tenant_id_number
    ON purchase_requisitions (tenant_id, number) WHERE is_deleted = false;

DROP INDEX IF EXISTS ix_glass_projects_tenant_id_code;
DROP INDEX IF EXISTS ux_glass_projects_tenant_code;
CREATE UNIQUE INDEX IF NOT EXISTS ix_glass_projects_tenant_id_code
    ON glass_projects (tenant_id, code) WHERE is_deleted = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ux_warranty_contracts_tenant_number;
CREATE UNIQUE INDEX IF NOT EXISTS ux_warranty_contracts_tenant_number
    ON warranty_contracts (tenant_id, number);

DROP INDEX IF EXISTS ix_purchase_requisitions_tenant_id_number;
CREATE UNIQUE INDEX IF NOT EXISTS ix_purchase_requisitions_tenant_id_number
    ON purchase_requisitions (tenant_id, number);

DROP INDEX IF EXISTS ix_glass_projects_tenant_id_code;
CREATE UNIQUE INDEX IF NOT EXISTS ix_glass_projects_tenant_id_code
    ON glass_projects (tenant_id, code);");
        }
    }
}

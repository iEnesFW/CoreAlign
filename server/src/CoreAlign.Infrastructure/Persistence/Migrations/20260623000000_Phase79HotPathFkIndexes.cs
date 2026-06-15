using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase79HotPathFkIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ix_orders_tenant_id_origin_dealer_account_id
    ON orders (tenant_id, origin_dealer_account_id);
CREATE INDEX IF NOT EXISTS ix_orders_tenant_id_glass_project_id
    ON orders (tenant_id, glass_project_id);
CREATE INDEX IF NOT EXISTS ix_glass_projects_tenant_id_assigned_salesperson_user_id
    ON glass_projects (tenant_id, assigned_salesperson_user_id);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_orders_tenant_id_origin_dealer_account_id;
DROP INDEX IF EXISTS ix_orders_tenant_id_glass_project_id;
DROP INDEX IF EXISTS ix_glass_projects_tenant_id_assigned_salesperson_user_id;");
        }
    }
}

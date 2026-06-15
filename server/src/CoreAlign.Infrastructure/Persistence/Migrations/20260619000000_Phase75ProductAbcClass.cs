using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 75 (MRP T4): ABC inventory classification. Adds products.abc_class (A/B/C/Unclassified),
    /// computed by ClassifyProductsAbcCommand from annual usage value. Idempotent: the column is
    /// added only if absent. DEFAULT 'Unclassified' backfills existing rows with a valid enum
    /// literal (an empty string would fail AbcClass enum parsing, mirroring procurement_type).
    /// </summary>
    public partial class Phase75ProductAbcClass : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE products ADD COLUMN IF NOT EXISTS abc_class character varying(20) NOT NULL DEFAULT 'Unclassified';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE products DROP COLUMN IF EXISTS abc_class;
");
        }
    }
}

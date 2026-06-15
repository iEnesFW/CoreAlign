using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CoreAlignDbContext))]
    [Migration("20260612000000_Phase68GlassConcurrencyTokenBackfill")]
    public partial class Phase68GlassConcurrencyTokenBackfill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE glass_projects ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql(
                "ALTER TABLE glass_field_surveys ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE glass_projects DROP COLUMN IF EXISTS concurrency_token;");
            migrationBuilder.Sql("ALTER TABLE glass_field_surveys DROP COLUMN IF EXISTS concurrency_token;");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase58FxRatesRetire : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "fx_rates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fx_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_date = table.Column<DateTime>(type: "date", nullable: false),
                    buying_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    selling_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    cross_rate_usd = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    fetched_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fx_rates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_fx_rates_code_date_source",
                table: "fx_rates",
                columns: new[] { "currency_code", "effective_date", "source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fx_rates_code_date_desc",
                table: "fx_rates",
                columns: new[] { "currency_code", "effective_date" },
                descending: new[] { false, true });
        }
    }
}

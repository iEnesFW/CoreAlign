using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase82FxRateScaleUnify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "vendor_ledger_entries",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "journal_lines",
                type: "numeric(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "fx_rate_to_base",
                table: "glass_projects",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "vendor_ledger_entries",
                type: "numeric(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "exchange_rate",
                table: "journal_lines",
                type: "numeric(18,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "fx_rate_to_base",
                table: "glass_projects",
                type: "numeric(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");
        }
    }
}

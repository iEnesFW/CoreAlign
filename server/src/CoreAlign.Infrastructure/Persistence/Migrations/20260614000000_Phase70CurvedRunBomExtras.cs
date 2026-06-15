using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase70CurvedRunBomExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "arc_glass_bent",
                table: "glass_project_runs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "bend_rail_fee_per_m",
                table: "glass_enclosure_settings_store",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 150m);

            migrationBuilder.AddColumn<decimal>(
                name: "bent_glass_cost_factor",
                table: "glass_enclosure_settings_store",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 2.75m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arc_glass_bent",
                table: "glass_project_runs");

            migrationBuilder.DropColumn(
                name: "bend_rail_fee_per_m",
                table: "glass_enclosure_settings_store");

            migrationBuilder.DropColumn(
                name: "bent_glass_cost_factor",
                table: "glass_enclosure_settings_store");
        }
    }
}

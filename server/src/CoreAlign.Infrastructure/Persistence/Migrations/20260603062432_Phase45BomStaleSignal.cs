using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase45BomStaleSignal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bom_stale_reason",
                table: "glass_projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_bom_stale",
                table: "glass_projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "stale_since_utc",
                table: "glass_projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_stale",
                table: "glass_projects",
                columns: new[] { "tenant_id", "is_bom_stale" },
                filter: "is_bom_stale = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_glass_projects_stale",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "bom_stale_reason",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "is_bom_stale",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "stale_since_utc",
                table: "glass_projects");
        }
    }
}

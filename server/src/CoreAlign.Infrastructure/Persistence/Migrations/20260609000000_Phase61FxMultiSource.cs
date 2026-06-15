using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase61FxMultiSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "fx_rate_snapshot",
                table: "invoices",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fx_source",
                table: "invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fx_locked_at_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "fx_rate_snapshot",
                table: "payments",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fx_source",
                table: "payments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fx_locked_at_utc",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id_fx_source",
                table: "invoices",
                columns: new[] { "tenant_id", "fx_source" },
                filter: "fx_source IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_fx_source",
                table: "payments",
                columns: new[] { "tenant_id", "fx_source" },
                filter: "fx_source IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_payments_tenant_id_fx_source", table: "payments");
            migrationBuilder.DropIndex(name: "ix_invoices_tenant_id_fx_source", table: "invoices");
            migrationBuilder.DropColumn(name: "fx_locked_at_utc", table: "payments");
            migrationBuilder.DropColumn(name: "fx_source", table: "payments");
            migrationBuilder.DropColumn(name: "fx_rate_snapshot", table: "payments");
            migrationBuilder.DropColumn(name: "fx_locked_at_utc", table: "invoices");
            migrationBuilder.DropColumn(name: "fx_source", table: "invoices");
            migrationBuilder.DropColumn(name: "fx_rate_snapshot", table: "invoices");
        }
    }
}

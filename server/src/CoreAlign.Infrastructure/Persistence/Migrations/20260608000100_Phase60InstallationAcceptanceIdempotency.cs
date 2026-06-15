using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase60InstallationAcceptanceIdempotency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accept_idempotency_key",
                table: "installation_acceptances",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_installation_acceptances_tenant_accept_idempotency_key",
                table: "installation_acceptances",
                columns: new[] { "tenant_id", "accept_idempotency_key" },
                unique: true,
                filter: "\"accept_idempotency_key\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_installation_acceptances_tenant_accept_idempotency_key",
                table: "installation_acceptances");

            migrationBuilder.DropColumn(
                name: "accept_idempotency_key",
                table: "installation_acceptances");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase24KvkkAndSurveyApplied : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "applied_at_utc",
                table: "glass_field_surveys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "data_subject_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    username_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    email_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_subject_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_data_subject_requests_tenant_id_user_id_request_type",
                table: "data_subject_requests",
                columns: new[] { "tenant_id", "user_id", "request_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "applied_at_utc",
                table: "glass_field_surveys");
        }
    }
}

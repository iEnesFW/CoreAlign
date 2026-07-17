using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase137ProductionJobLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_job_logs CASCADE;");
            
            migrationBuilder.CreateTable(
                name: "production_job_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_job_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_job_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_job_logs_production_job_steps_production_job_ste~",
                        column: x => x.production_job_step_id,
                        principalTable: "production_job_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_job_logs_production_jobs_production_job_id",
                        column: x => x.production_job_id,
                        principalTable: "production_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_job_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "code",
                keyValue: "GBP",
                column: "name",
                value: "İngiliz Sterlini");

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "code",
                keyValue: "TRY",
                columns: new[] { "name", "symbol" },
                values: new object[] { "Türk Lirası", "₺" });

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "code",
                keyValue: "USD",
                column: "name",
                value: "ABD Doları");

            migrationBuilder.UpdateData(
                table: "districts",
                keyColumn: "id",
                keyValue: 3401,
                column: "name",
                value: "Kadıköy");

            migrationBuilder.UpdateData(
                table: "districts",
                keyColumn: "id",
                keyValue: 3402,
                column: "name",
                value: "Beşiktaş");

            migrationBuilder.UpdateData(
                table: "districts",
                keyColumn: "id",
                keyValue: 3403,
                column: "name",
                value: "Şişli");

            migrationBuilder.UpdateData(
                table: "provinces",
                keyColumn: "id",
                keyValue: 34,
                column: "name",
                value: "İstanbul");

            migrationBuilder.UpdateData(
                table: "provinces",
                keyColumn: "id",
                keyValue: 35,
                column: "name",
                value: "İzmir");

            migrationBuilder.CreateIndex(
                name: "ix_production_job_logs_production_job_id",
                table: "production_job_logs",
                column: "production_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_job_logs_production_job_step_id",
                table: "production_job_logs",
                column: "production_job_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_job_logs_tenant_id",
                table: "production_job_logs",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_job_logs");

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "code",
                keyValue: "GBP",
                column: "name",
                value: "Ingiliz Sterlini");

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "code",
                keyValue: "TRY",
                columns: new[] { "name", "symbol" },
                values: new object[] { "Türk Lirasi", "?" });

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "code",
                keyValue: "USD",
                column: "name",
                value: "ABD Dolari");

            migrationBuilder.UpdateData(
                table: "districts",
                keyColumn: "id",
                keyValue: 3401,
                column: "name",
                value: "Kadiköy");

            migrationBuilder.UpdateData(
                table: "districts",
                keyColumn: "id",
                keyValue: 3402,
                column: "name",
                value: "Besiktas");

            migrationBuilder.UpdateData(
                table: "districts",
                keyColumn: "id",
                keyValue: 3403,
                column: "name",
                value: "Sisli");

            migrationBuilder.UpdateData(
                table: "provinces",
                keyColumn: "id",
                keyValue: 34,
                column: "name",
                value: "Istanbul");

            migrationBuilder.UpdateData(
                table: "provinces",
                keyColumn: "id",
                keyValue: 35,
                column: "name",
                value: "Izmir");
        }
    }
}

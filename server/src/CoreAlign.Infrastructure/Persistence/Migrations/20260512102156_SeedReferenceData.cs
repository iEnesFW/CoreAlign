using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at_utc", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tenant administrator with full access.", true, "TenantAdmin" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard user.", true, "User" }
                });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "id", "created_at_utc", "display_name", "is_active", "max_projects", "max_users", "name", "price_monthly", "price_yearly", "trial_duration_days" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Free Trial", true, 5, 3, "FreeTrial", 0m, 0m, 14 },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard", true, 50, 10, "Standard", 29m, 290m, 0 },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Professional", true, 500, 50, "Pro", 99m, 990m, 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3);
        }
    }
}

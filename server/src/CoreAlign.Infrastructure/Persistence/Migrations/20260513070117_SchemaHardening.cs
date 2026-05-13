using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchemaHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_customer_contacts_tenant_id_customer_id",
                table: "customer_contacts");

            migrationBuilder.DropIndex(
                name: "ix_customer_addresses_tenant_id_customer_id",
                table: "customer_addresses");

            migrationBuilder.DropIndex(
                name: "ix_activity_logs_user_id",
                table: "activity_logs");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_email_unique",
                table: "customers",
                columns: new[] { "tenant_id", "email" },
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_tax_number_unique",
                table: "customers",
                columns: new[] { "tenant_id", "tax_number" },
                unique: true,
                filter: "tax_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_contacts_primary_unique",
                table: "customer_contacts",
                columns: new[] { "tenant_id", "customer_id" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_primary_unique",
                table: "customer_addresses",
                columns: new[] { "tenant_id", "customer_id" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_tenant_id_user_id_created_at_utc",
                table: "activity_logs",
                columns: new[] { "tenant_id", "user_id", "created_at_utc" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_email_unique",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_tax_number_unique",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customer_contacts_primary_unique",
                table: "customer_contacts");

            migrationBuilder.DropIndex(
                name: "ix_customer_addresses_primary_unique",
                table: "customer_addresses");

            migrationBuilder.DropIndex(
                name: "ix_activity_logs_tenant_id_user_id_created_at_utc",
                table: "activity_logs");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_customer_contacts_tenant_id_customer_id",
                table: "customer_contacts",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_tenant_id_customer_id",
                table: "customer_addresses",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_user_id",
                table: "activity_logs",
                column: "user_id");
        }
    }
}

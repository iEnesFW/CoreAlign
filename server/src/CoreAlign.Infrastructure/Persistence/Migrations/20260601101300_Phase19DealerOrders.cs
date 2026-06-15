using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Faz D / Phase 19 — adds dealer-routed B2B order origin + 3-way approval
    /// fields to the orders table, plus two supporting indexes for the
    /// "customer pending approvals" queue and the dealer's "my orders" view.
    /// All new columns are nullable except <c>origin_persona</c>, which defaults
    /// to <c>"tenant"</c> so existing rows remain valid.
    /// </summary>
    public partial class Phase19DealerOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "origin_persona",
                table: "orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "tenant");

            migrationBuilder.AddColumn<Guid>(
                name: "origin_customer_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_dealer_account_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_dealer_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dealer_approval_status",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "dealer_approved_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dealer_approved_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dealer_rejection_reason",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_dealer_approval_status_created_at_utc",
                table: "orders",
                columns: new[] { "tenant_id", "dealer_approval_status", "created_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_origin_dealer_account_id_created_at_utc",
                table: "orders",
                columns: new[] { "tenant_id", "origin_dealer_account_id", "created_at_utc" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_origin_dealer_account_id_created_at_utc",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_dealer_approval_status_created_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "dealer_rejection_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "dealer_approved_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "dealer_approved_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "dealer_approval_status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "origin_dealer_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "origin_dealer_account_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "origin_customer_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "origin_persona",
                table: "orders");
        }
    }
}

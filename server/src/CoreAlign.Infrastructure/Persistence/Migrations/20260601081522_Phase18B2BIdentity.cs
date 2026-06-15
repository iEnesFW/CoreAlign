using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase18B2BIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_users_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dealer_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tax_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealer_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dealer_customer_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealer_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealer_customer_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_dealer_customer_links_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dealer_customer_links_dealer_accounts_dealer_account_id",
                        column: x => x.dealer_account_id,
                        principalTable: "dealer_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dealer_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealer_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealer_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_dealer_users_dealer_accounts_dealer_account_id",
                        column: x => x.dealer_account_id,
                        principalTable: "dealer_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dealer_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_users_customer_id",
                table: "customer_users",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_users_tenant_id_customer_id",
                table: "customer_users",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_users_tenant_id_customer_id_user_id",
                table: "customer_users",
                columns: new[] { "tenant_id", "customer_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_users_tenant_id_user_id",
                table: "customer_users",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_users_user_id",
                table: "customer_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_dealer_accounts_tenant_id_code",
                table: "dealer_accounts",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dealer_accounts_tenant_id_status",
                table: "dealer_accounts",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_customer_links_customer_id",
                table: "dealer_customer_links",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_dealer_customer_links_dealer_account_id",
                table: "dealer_customer_links",
                column: "dealer_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_dealer_customer_links_tenant_id_customer_id",
                table: "dealer_customer_links",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_customer_links_tenant_id_dealer_account_id",
                table: "dealer_customer_links",
                columns: new[] { "tenant_id", "dealer_account_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_customer_links_tenant_id_dealer_account_id_customer_~",
                table: "dealer_customer_links",
                columns: new[] { "tenant_id", "dealer_account_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dealer_users_dealer_account_id",
                table: "dealer_users",
                column: "dealer_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_dealer_users_tenant_id_dealer_account_id",
                table: "dealer_users",
                columns: new[] { "tenant_id", "dealer_account_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_users_tenant_id_dealer_account_id_user_id",
                table: "dealer_users",
                columns: new[] { "tenant_id", "dealer_account_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dealer_users_tenant_id_user_id",
                table: "dealer_users",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_users_user_id",
                table: "dealer_users",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_users");

            migrationBuilder.DropTable(
                name: "dealer_customer_links");

            migrationBuilder.DropTable(
                name: "dealer_users");

            migrationBuilder.DropTable(
                name: "dealer_accounts");
        }
    }
}

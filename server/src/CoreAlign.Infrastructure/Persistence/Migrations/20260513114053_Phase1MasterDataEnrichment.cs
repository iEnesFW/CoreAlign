using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1MasterDataEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_is_active",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_id_is_active",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "products");

            migrationBuilder.AddColumn<bool>(
                name: "is_stock_tracked",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "average_cost",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "barcode",
                table: "products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "base_uom_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "depth_cm",
                table: "products",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_of_life_date",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height_cm",
                table: "products",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_lot_tracked",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_price_tax_inclusive",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_serial_tracked",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "last_purchase_cost",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "launch_date",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lead_time_days",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "list_price",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "max_stock",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "min_selling_price",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "min_stock",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "mpn",
                table: "products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_product_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "purchase_uom_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "reorder_point",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "safety_stock",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_uom_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_description",
                table: "products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "standard_cost",
                table: "products",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "tags_json",
                table: "products",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_rate_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_attributes_json",
                table: "products",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "volume_m3",
                table: "products",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_kg",
                table: "products",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "width_cm",
                table: "products",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "block_reason",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification",
                table: "customers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "credit_limit",
                table: "customers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "current_balance",
                table: "customers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_group_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_currency",
                table: "customers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<decimal>(
                name: "default_discount_percent",
                table: "customers",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "language_code",
                table: "customers",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overdue_amount",
                table: "customers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_customer_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_terms_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "price_list_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_rep_user_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "tax_office",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "territory",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trade_name",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Business");

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_discount_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    current_year = table.Column<int>(type: "integer", nullable: false),
                    next_number = table.Column<long>(type: "bigint", nullable: false),
                    pad_length = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_terms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    net_days = table.Column<int>(type: "integer", nullable: false),
                    discount_days = table.Column<int>(type: "integer", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    end_of_month = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_terms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_tax_inclusive = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_categories_product_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    is_withholding = table.Column<bool>(type: "boolean", nullable: false),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units_of_measure",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversion_factor = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units_of_measure", x => x.id);
                    table.ForeignKey(
                        name: "fk_units_of_measure_units_of_measure_base_uom_id",
                        column: x => x.base_uom_id,
                        principalTable: "units_of_measure",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    min_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    max_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    discount_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_list_items_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_price_list_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_base_uom_id",
                table: "products",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_id",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_parent_product_id",
                table: "products",
                column: "parent_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_tax_rate_id",
                table: "products",
                column: "tax_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_barcode_unique",
                table: "products",
                columns: new[] { "tenant_id", "barcode" },
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_brand_id",
                table: "products",
                columns: new[] { "tenant_id", "brand_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_category_id",
                table: "products",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_status",
                table: "products",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_customer_group_id",
                table: "customers",
                column: "customer_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_parent_customer_id",
                table: "customers",
                column: "parent_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_payment_terms_id",
                table: "customers",
                column: "payment_terms_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_price_list_id",
                table: "customers",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_code_unique",
                table: "customers",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_id_customer_group_id",
                table: "customers",
                columns: new[] { "tenant_id", "customer_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_id_status",
                table: "customers",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_id_code",
                table: "brands",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_id_name",
                table: "brands",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_groups_tenant_id_code",
                table: "customer_groups",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_sequences_tenant_id_type",
                table: "document_sequences",
                columns: new[] { "tenant_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_terms_tenant_id_code",
                table: "payment_terms",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_price_list_id",
                table: "price_list_items",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_product_id",
                table: "price_list_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_tenant_id_price_list_id_product_id",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_price_lists_tenant_id_code",
                table: "price_lists",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_lists_tenant_id_is_default",
                table: "price_lists",
                columns: new[] { "tenant_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_parent_category_id",
                table: "product_categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_tenant_id_code",
                table: "product_categories",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_tenant_id_parent_category_id",
                table: "product_categories",
                columns: new[] { "tenant_id", "parent_category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_tenant_id_code",
                table: "tax_rates",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_tenant_id_is_withholding",
                table: "tax_rates",
                columns: new[] { "tenant_id", "is_withholding" });

            migrationBuilder.CreateIndex(
                name: "ix_units_of_measure_base_uom_id",
                table: "units_of_measure",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_units_of_measure_tenant_id_code",
                table: "units_of_measure",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_tenant_id_code",
                table: "warehouses",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_tenant_id_is_default",
                table: "warehouses",
                columns: new[] { "tenant_id", "is_default" });

            migrationBuilder.AddForeignKey(
                name: "fk_customers_customer_groups_customer_group_id",
                table: "customers",
                column: "customer_group_id",
                principalTable: "customer_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_customers_parent_customer_id",
                table: "customers",
                column: "parent_customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_payment_terms_payment_terms_id",
                table: "customers",
                column: "payment_terms_id",
                principalTable: "payment_terms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_price_lists_price_list_id",
                table: "customers",
                column: "price_list_id",
                principalTable: "price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_brands_brand_id",
                table: "products",
                column: "brand_id",
                principalTable: "brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "product_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_products_parent_product_id",
                table: "products",
                column: "parent_product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_tax_rates_tax_rate_id",
                table: "products",
                column: "tax_rate_id",
                principalTable: "tax_rates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_units_of_measure_base_uom_id",
                table: "products",
                column: "base_uom_id",
                principalTable: "units_of_measure",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customers_customer_groups_customer_group_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_customers_parent_customer_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_payment_terms_payment_terms_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_price_lists_price_list_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_products_brands_brand_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_product_categories_category_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_products_parent_product_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_tax_rates_tax_rate_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_units_of_measure_base_uom_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "customer_groups");

            migrationBuilder.DropTable(
                name: "document_sequences");

            migrationBuilder.DropTable(
                name: "payment_terms");

            migrationBuilder.DropTable(
                name: "price_list_items");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropTable(
                name: "tax_rates");

            migrationBuilder.DropTable(
                name: "units_of_measure");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropTable(
                name: "price_lists");

            migrationBuilder.DropIndex(
                name: "ix_products_base_uom_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_brand_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_category_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_parent_product_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tax_rate_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_barcode_unique",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_brand_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_category_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_status",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_customers_customer_group_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_parent_customer_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_payment_terms_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_price_list_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_code_unique",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_id_customer_group_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_id_status",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "average_cost",
                table: "products");

            migrationBuilder.DropColumn(
                name: "barcode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "base_uom_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "brand_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "depth_cm",
                table: "products");

            migrationBuilder.DropColumn(
                name: "end_of_life_date",
                table: "products");

            migrationBuilder.DropColumn(
                name: "height_cm",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_lot_tracked",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_price_tax_inclusive",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_serial_tracked",
                table: "products");

            migrationBuilder.DropColumn(
                name: "last_purchase_cost",
                table: "products");

            migrationBuilder.DropColumn(
                name: "launch_date",
                table: "products");

            migrationBuilder.DropColumn(
                name: "lead_time_days",
                table: "products");

            migrationBuilder.DropColumn(
                name: "list_price",
                table: "products");

            migrationBuilder.DropColumn(
                name: "max_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "min_selling_price",
                table: "products");

            migrationBuilder.DropColumn(
                name: "min_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "mpn",
                table: "products");

            migrationBuilder.DropColumn(
                name: "parent_product_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "purchase_uom_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "reorder_point",
                table: "products");

            migrationBuilder.DropColumn(
                name: "safety_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sales_uom_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "short_description",
                table: "products");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "products");

            migrationBuilder.DropColumn(
                name: "standard_cost",
                table: "products");

            migrationBuilder.DropColumn(
                name: "status",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tags_json",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tax_rate_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "variant_attributes_json",
                table: "products");

            migrationBuilder.DropColumn(
                name: "volume_m3",
                table: "products");

            migrationBuilder.DropColumn(
                name: "weight_kg",
                table: "products");

            migrationBuilder.DropColumn(
                name: "width_cm",
                table: "products");

            migrationBuilder.DropColumn(
                name: "block_reason",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "classification",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "code",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "credit_limit",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "current_balance",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "customer_group_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "default_currency",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "default_discount_percent",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "language_code",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "legal_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "national_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "overdue_amount",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "parent_customer_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "payment_terms_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "sales_rep_user_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "status",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "tax_office",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "territory",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "trade_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "type",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "website",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_stock_tracked",
                table: "products");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_is_active",
                table: "products",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_id_is_active",
                table: "customers",
                columns: new[] { "tenant_id", "is_active" });
        }
    }
}

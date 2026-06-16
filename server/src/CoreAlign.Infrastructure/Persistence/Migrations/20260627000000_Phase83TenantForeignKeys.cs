using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase83TenantForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_vendor_bill_lines_tenant_id",
                table: "vendor_bill_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_count_lines_tenant_id",
                table: "stock_count_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_tenant_id",
                table: "shipment_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_request_lines_tenant_id",
                table: "return_request_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_lines_tenant_id",
                table: "quote_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_tenant_id",
                table: "purchase_order_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_template_reviews_tenant_id",
                table: "project_template_reviews",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_template_lines_tenant_id",
                table: "order_template_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_tenant_id",
                table: "invoice_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl_posting_mappings_tenant_id",
                table: "gl_posting_mappings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_tickets_tenant_id",
                table: "feedback_tickets",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_accounting_periods_tenants_tenant_id",
                table: "accounting_periods",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_activity_logs_tenants_tenant_id",
                table: "activity_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_brands_tenants_tenant_id",
                table: "brands",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_comments_tenants_tenant_id",
                table: "comments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_addresses_tenants_tenant_id",
                table: "customer_addresses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_contacts_tenants_tenant_id",
                table: "customer_contacts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_dealer_product_visibilities_tenants_tenant_id",
                table: "customer_dealer_product_visibilities",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_groups_tenants_tenant_id",
                table: "customer_groups",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_ledger_entries_tenants_tenant_id",
                table: "customer_ledger_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_merge_logs_tenants_tenant_id",
                table: "customer_merge_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_product_prices_tenants_tenant_id",
                table: "customer_product_prices",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_tag_links_tenants_tenant_id",
                table: "customer_tag_links",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_transactions_tenants_tenant_id",
                table: "customer_transactions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_users_tenants_tenant_id",
                table: "customer_users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_tenants_tenant_id",
                table: "customers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dashboard_widgets_tenants_tenant_id",
                table: "dashboard_widgets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_data_subject_requests_tenants_tenant_id",
                table: "data_subject_requests",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dealer_accounts_tenants_tenant_id",
                table: "dealer_accounts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dealer_commission_ledger_entries_tenants_tenant_id",
                table: "dealer_commission_ledger_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dealer_customer_links_tenants_tenant_id",
                table: "dealer_customer_links",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dealer_users_tenants_tenant_id",
                table: "dealer_users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_document_sequences_tenants_tenant_id",
                table: "document_sequences",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_email_templates_tenants_tenant_id",
                table: "email_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_entity_audit_logs_tenants_tenant_id",
                table: "entity_audit_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_exchange_rates_tenants_tenant_id",
                table: "exchange_rates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_external_user_bindings_tenants_tenant_id",
                table: "external_user_bindings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_feedback_tickets_tenants_tenant_id",
                table: "feedback_tickets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_gl_accounts_tenants_tenant_id",
                table: "gl_accounts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_gl_posting_mappings_tenants_tenant_id",
                table: "gl_posting_mappings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_brand_vendors_tenants_tenant_id",
                table: "glass_brand_vendors",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_color_options_tenants_tenant_id",
                table: "glass_color_options",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_discount_rules_tenants_tenant_id",
                table: "glass_discount_rules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_enclosure_settings_store_tenants_tenant_id",
                table: "glass_enclosure_settings_store",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_field_surveys_tenants_tenant_id",
                table: "glass_field_surveys",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_hardware_items_tenants_tenant_id",
                table: "glass_hardware_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_hardware_kits_tenants_tenant_id",
                table: "glass_hardware_kits",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_notification_logs_tenants_tenant_id",
                table: "glass_notification_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_notification_templates_tenants_tenant_id",
                table: "glass_notification_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_profile_items_tenants_tenant_id",
                table: "glass_profile_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_profile_systems_tenants_tenant_id",
                table: "glass_profile_systems",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_attachments_tenants_tenant_id",
                table: "glass_project_attachments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_bom_lines_tenants_tenant_id",
                table: "glass_project_bom_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_change_logs_tenants_tenant_id",
                table: "glass_project_change_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_cutting_plans_tenants_tenant_id",
                table: "glass_project_cutting_plans",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_order_links_tenants_tenant_id",
                table: "glass_project_order_links",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_panels_tenants_tenant_id",
                table: "glass_project_panels",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_quote_snapshots_tenants_tenant_id",
                table: "glass_project_quote_snapshots",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_runs_tenants_tenant_id",
                table: "glass_project_runs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_scenes_tenants_tenant_id",
                table: "glass_project_scenes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_share_tokens_tenants_tenant_id",
                table: "glass_project_share_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_projects_tenants_tenant_id",
                table: "glass_projects",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_run_connections_tenants_tenant_id",
                table: "glass_run_connections",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_types_tenants_tenant_id",
                table: "glass_types",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_work_order_revisions_tenants_tenant_id",
                table: "glass_work_order_revisions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_glass_work_orders_tenants_tenant_id",
                table: "glass_work_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_kit_item_tenants_tenant_id",
                table: "hardware_kit_item",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_installation_acceptances_tenants_tenant_id",
                table: "installation_acceptances",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invoice_lines_tenants_tenant_id",
                table: "invoice_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_tenants_tenant_id",
                table: "invoices",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_journal_entries_tenants_tenant_id",
                table: "journal_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_journal_lines_tenants_tenant_id",
                table: "journal_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_lots_tenants_tenant_id",
                table: "lots",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_maintenance_schedules_tenants_tenant_id",
                table: "maintenance_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_mrp_action_messages_tenants_tenant_id",
                table: "mrp_action_messages",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_mrp_peggings_tenants_tenant_id",
                table: "mrp_peggings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_mrp_plan_runs_tenants_tenant_id",
                table: "mrp_plan_runs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_mrp_planned_orders_tenants_tenant_id",
                table: "mrp_planned_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_tenants_tenant_id",
                table: "notification_messages",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_tenants_tenant_id",
                table: "notification_preferences",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_rate_counters_tenants_tenant_id",
                table: "notification_rate_counters",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_tenants_tenant_id",
                table: "notifications",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_order_lines_tenants_tenant_id",
                table: "order_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_order_revisions_tenants_tenant_id",
                table: "order_revisions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_order_template_lines_tenants_tenant_id",
                table: "order_template_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_order_templates_tenants_tenant_id",
                table: "order_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_tenants_tenant_id",
                table: "orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_messages_tenants_tenant_id",
                table: "outbox_messages",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_password_histories_tenants_tenant_id",
                table: "password_histories",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_applications_tenants_tenant_id",
                table: "payment_applications",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_attempts_tenants_tenant_id",
                table: "payment_attempts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_sessions_tenants_tenant_id",
                table: "payment_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_terms_tenants_tenant_id",
                table: "payment_terms",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_tenants_tenant_id",
                table: "payment_transactions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payments_tenants_tenant_id",
                table: "payments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_planned_production_orders_tenants_tenant_id",
                table: "planned_production_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_tenants_tenant_id",
                table: "price_list_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_lists_tenants_tenant_id",
                table: "price_lists",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pricing_discount_rules_tenants_tenant_id",
                table: "pricing_discount_rules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pricing_tax_rules_tenants_tenant_id",
                table: "pricing_tax_rules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_categories_tenants_tenant_id",
                table: "product_categories",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_components_tenants_tenant_id",
                table: "product_components",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_images_tenants_tenant_id",
                table: "product_images",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_substitutes_tenants_tenant_id",
                table: "product_substitutes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variants_tenants_tenant_id",
                table: "product_variants",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_tenants_tenant_id",
                table: "products",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_template_installs_tenants_tenant_id",
                table: "project_template_installs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_template_reviews_tenants_tenant_id",
                table: "project_template_reviews",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_template_run_preset_tenants_tenant_id",
                table: "project_template_run_preset",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_templates_tenants_tenant_id",
                table: "project_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_provider_webhook_inbox_tenants_tenant_id",
                table: "provider_webhook_inbox",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_punch_list_items_tenants_tenant_id",
                table: "punch_list_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_order_lines_tenants_tenant_id",
                table: "purchase_order_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_tenants_tenant_id",
                table: "purchase_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_requisition_lines_tenants_tenant_id",
                table: "purchase_requisition_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_requisitions_tenants_tenant_id",
                table: "purchase_requisitions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_quote_lines_tenants_tenant_id",
                table: "quote_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_quotes_tenants_tenant_id",
                table: "quotes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_report_definitions_tenants_tenant_id",
                table: "report_definitions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_report_runs_tenants_tenant_id",
                table: "report_runs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_report_schedules_tenants_tenant_id",
                table: "report_schedules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_retention_policies_tenants_tenant_id",
                table: "retention_policies",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_request_lines_tenants_tenant_id",
                table: "return_request_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_requests_tenants_tenant_id",
                table: "return_requests",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_saved_reports_tenants_tenant_id",
                table: "saved_reports",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_service_tickets_tenants_tenant_id",
                table: "service_tickets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shipment_lines_tenants_tenant_id",
                table: "shipment_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shipments_tenants_tenant_id",
                table: "shipments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_allocations_tenants_tenant_id",
                table: "stock_allocations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_count_lines_tenants_tenant_id",
                table: "stock_count_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_counts_tenants_tenant_id",
                table: "stock_counts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_items_tenants_tenant_id",
                table: "stock_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_tenants_tenant_id",
                table: "stock_movements",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_reason_codes_tenants_tenant_id",
                table: "stock_reason_codes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transactions_tenants_tenant_id",
                table: "stock_transactions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_order_items_tenants_tenant_id",
                table: "subscription_order_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_orders_tenants_tenant_id",
                table: "subscription_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tags_tenants_tenant_id",
                table: "tags",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_declaration_lines_tenants_tenant_id",
                table: "tax_declaration_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_declarations_tenants_tenant_id",
                table: "tax_declarations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_rates_tenants_tenant_id",
                table: "tax_rates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_identity_providers_tenants_tenant_id",
                table: "tenant_identity_providers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_modules_tenants_tenant_id",
                table: "tenant_modules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_provider_configs_tenants_tenant_id",
                table: "tenant_provider_configs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_settings_store_tenants_tenant_id",
                table: "tenant_settings_store",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_theme_assets_tenants_tenant_id",
                table: "tenant_theme_assets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_two_factor_backup_codes_tenants_tenant_id",
                table: "two_factor_backup_codes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_two_factor_challenges_tenants_tenant_id",
                table: "two_factor_challenges",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_units_of_measure_tenants_tenant_id",
                table: "units_of_measure",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_consents_tenants_tenant_id",
                table: "user_consents",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_device_tokens_tenants_tenant_id",
                table: "user_device_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_notification_preferences_tenants_tenant_id",
                table: "user_notification_preferences",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_addresses_tenants_tenant_id",
                table: "vendor_addresses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_bank_accounts_tenants_tenant_id",
                table: "vendor_bank_accounts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_bill_lines_tenants_tenant_id",
                table: "vendor_bill_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_bills_tenants_tenant_id",
                table: "vendor_bills",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_contacts_tenants_tenant_id",
                table: "vendor_contacts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_ledger_entries_tenants_tenant_id",
                table: "vendor_ledger_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_payment_applications_tenants_tenant_id",
                table: "vendor_payment_applications",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_payments_tenants_tenant_id",
                table: "vendor_payments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendors_tenants_tenant_id",
                table: "vendors",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_warehouses_tenants_tenant_id",
                table: "warehouses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_warranty_contracts_tenants_tenant_id",
                table: "warranty_contracts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_work_centers_tenants_tenant_id",
                table: "work_centers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_accounting_periods_tenants_tenant_id",
                table: "accounting_periods");

            migrationBuilder.DropForeignKey(
                name: "fk_activity_logs_tenants_tenant_id",
                table: "activity_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_brands_tenants_tenant_id",
                table: "brands");

            migrationBuilder.DropForeignKey(
                name: "fk_comments_tenants_tenant_id",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_addresses_tenants_tenant_id",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_contacts_tenants_tenant_id",
                table: "customer_contacts");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_dealer_product_visibilities_tenants_tenant_id",
                table: "customer_dealer_product_visibilities");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_groups_tenants_tenant_id",
                table: "customer_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_ledger_entries_tenants_tenant_id",
                table: "customer_ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_merge_logs_tenants_tenant_id",
                table: "customer_merge_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_product_prices_tenants_tenant_id",
                table: "customer_product_prices");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_tag_links_tenants_tenant_id",
                table: "customer_tag_links");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_transactions_tenants_tenant_id",
                table: "customer_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_users_tenants_tenant_id",
                table: "customer_users");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_tenants_tenant_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_dashboard_widgets_tenants_tenant_id",
                table: "dashboard_widgets");

            migrationBuilder.DropForeignKey(
                name: "fk_data_subject_requests_tenants_tenant_id",
                table: "data_subject_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_dealer_accounts_tenants_tenant_id",
                table: "dealer_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_dealer_commission_ledger_entries_tenants_tenant_id",
                table: "dealer_commission_ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_dealer_customer_links_tenants_tenant_id",
                table: "dealer_customer_links");

            migrationBuilder.DropForeignKey(
                name: "fk_dealer_users_tenants_tenant_id",
                table: "dealer_users");

            migrationBuilder.DropForeignKey(
                name: "fk_document_sequences_tenants_tenant_id",
                table: "document_sequences");

            migrationBuilder.DropForeignKey(
                name: "fk_email_templates_tenants_tenant_id",
                table: "email_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_entity_audit_logs_tenants_tenant_id",
                table: "entity_audit_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_exchange_rates_tenants_tenant_id",
                table: "exchange_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_external_user_bindings_tenants_tenant_id",
                table: "external_user_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_feedback_tickets_tenants_tenant_id",
                table: "feedback_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_gl_accounts_tenants_tenant_id",
                table: "gl_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_gl_posting_mappings_tenants_tenant_id",
                table: "gl_posting_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_brand_vendors_tenants_tenant_id",
                table: "glass_brand_vendors");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_color_options_tenants_tenant_id",
                table: "glass_color_options");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_discount_rules_tenants_tenant_id",
                table: "glass_discount_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_enclosure_settings_store_tenants_tenant_id",
                table: "glass_enclosure_settings_store");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_field_surveys_tenants_tenant_id",
                table: "glass_field_surveys");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_hardware_items_tenants_tenant_id",
                table: "glass_hardware_items");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_hardware_kits_tenants_tenant_id",
                table: "glass_hardware_kits");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_notification_logs_tenants_tenant_id",
                table: "glass_notification_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_notification_templates_tenants_tenant_id",
                table: "glass_notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_profile_items_tenants_tenant_id",
                table: "glass_profile_items");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_profile_systems_tenants_tenant_id",
                table: "glass_profile_systems");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_attachments_tenants_tenant_id",
                table: "glass_project_attachments");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_bom_lines_tenants_tenant_id",
                table: "glass_project_bom_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_change_logs_tenants_tenant_id",
                table: "glass_project_change_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_cutting_plans_tenants_tenant_id",
                table: "glass_project_cutting_plans");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_order_links_tenants_tenant_id",
                table: "glass_project_order_links");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_panels_tenants_tenant_id",
                table: "glass_project_panels");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_quote_snapshots_tenants_tenant_id",
                table: "glass_project_quote_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_runs_tenants_tenant_id",
                table: "glass_project_runs");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_scenes_tenants_tenant_id",
                table: "glass_project_scenes");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_share_tokens_tenants_tenant_id",
                table: "glass_project_share_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_projects_tenants_tenant_id",
                table: "glass_projects");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_run_connections_tenants_tenant_id",
                table: "glass_run_connections");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_types_tenants_tenant_id",
                table: "glass_types");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_work_order_revisions_tenants_tenant_id",
                table: "glass_work_order_revisions");

            migrationBuilder.DropForeignKey(
                name: "fk_glass_work_orders_tenants_tenant_id",
                table: "glass_work_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_kit_item_tenants_tenant_id",
                table: "hardware_kit_item");

            migrationBuilder.DropForeignKey(
                name: "fk_installation_acceptances_tenants_tenant_id",
                table: "installation_acceptances");

            migrationBuilder.DropForeignKey(
                name: "fk_invoice_lines_tenants_tenant_id",
                table: "invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_invoices_tenants_tenant_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_journal_entries_tenants_tenant_id",
                table: "journal_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_journal_lines_tenants_tenant_id",
                table: "journal_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_lots_tenants_tenant_id",
                table: "lots");

            migrationBuilder.DropForeignKey(
                name: "fk_maintenance_schedules_tenants_tenant_id",
                table: "maintenance_schedules");

            migrationBuilder.DropForeignKey(
                name: "fk_mrp_action_messages_tenants_tenant_id",
                table: "mrp_action_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_mrp_peggings_tenants_tenant_id",
                table: "mrp_peggings");

            migrationBuilder.DropForeignKey(
                name: "fk_mrp_plan_runs_tenants_tenant_id",
                table: "mrp_plan_runs");

            migrationBuilder.DropForeignKey(
                name: "fk_mrp_planned_orders_tenants_tenant_id",
                table: "mrp_planned_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_tenants_tenant_id",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_tenants_tenant_id",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_rate_counters_tenants_tenant_id",
                table: "notification_rate_counters");

            migrationBuilder.DropForeignKey(
                name: "fk_notifications_tenants_tenant_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "fk_order_lines_tenants_tenant_id",
                table: "order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_order_revisions_tenants_tenant_id",
                table: "order_revisions");

            migrationBuilder.DropForeignKey(
                name: "fk_order_template_lines_tenants_tenant_id",
                table: "order_template_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_order_templates_tenants_tenant_id",
                table: "order_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_orders_tenants_tenant_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_outbox_messages_tenants_tenant_id",
                table: "outbox_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_password_histories_tenants_tenant_id",
                table: "password_histories");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_applications_tenants_tenant_id",
                table: "payment_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_attempts_tenants_tenant_id",
                table: "payment_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_sessions_tenants_tenant_id",
                table: "payment_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_terms_tenants_tenant_id",
                table: "payment_terms");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_tenants_tenant_id",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_payments_tenants_tenant_id",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "fk_planned_production_orders_tenants_tenant_id",
                table: "planned_production_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_tenants_tenant_id",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_lists_tenants_tenant_id",
                table: "price_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_pricing_discount_rules_tenants_tenant_id",
                table: "pricing_discount_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_pricing_tax_rules_tenants_tenant_id",
                table: "pricing_tax_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_product_categories_tenants_tenant_id",
                table: "product_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_product_components_tenants_tenant_id",
                table: "product_components");

            migrationBuilder.DropForeignKey(
                name: "fk_product_images_tenants_tenant_id",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "fk_product_substitutes_tenants_tenant_id",
                table: "product_substitutes");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variants_tenants_tenant_id",
                table: "product_variants");

            migrationBuilder.DropForeignKey(
                name: "fk_products_tenants_tenant_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_project_template_installs_tenants_tenant_id",
                table: "project_template_installs");

            migrationBuilder.DropForeignKey(
                name: "fk_project_template_reviews_tenants_tenant_id",
                table: "project_template_reviews");

            migrationBuilder.DropForeignKey(
                name: "fk_project_template_run_preset_tenants_tenant_id",
                table: "project_template_run_preset");

            migrationBuilder.DropForeignKey(
                name: "fk_project_templates_tenants_tenant_id",
                table: "project_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_provider_webhook_inbox_tenants_tenant_id",
                table: "provider_webhook_inbox");

            migrationBuilder.DropForeignKey(
                name: "fk_punch_list_items_tenants_tenant_id",
                table: "punch_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_order_lines_tenants_tenant_id",
                table: "purchase_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_tenants_tenant_id",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_requisition_lines_tenants_tenant_id",
                table: "purchase_requisition_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_requisitions_tenants_tenant_id",
                table: "purchase_requisitions");

            migrationBuilder.DropForeignKey(
                name: "fk_quote_lines_tenants_tenant_id",
                table: "quote_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_quotes_tenants_tenant_id",
                table: "quotes");

            migrationBuilder.DropForeignKey(
                name: "fk_report_definitions_tenants_tenant_id",
                table: "report_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_report_runs_tenants_tenant_id",
                table: "report_runs");

            migrationBuilder.DropForeignKey(
                name: "fk_report_schedules_tenants_tenant_id",
                table: "report_schedules");

            migrationBuilder.DropForeignKey(
                name: "fk_retention_policies_tenants_tenant_id",
                table: "retention_policies");

            migrationBuilder.DropForeignKey(
                name: "fk_return_request_lines_tenants_tenant_id",
                table: "return_request_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_return_requests_tenants_tenant_id",
                table: "return_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_saved_reports_tenants_tenant_id",
                table: "saved_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_service_tickets_tenants_tenant_id",
                table: "service_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_shipment_lines_tenants_tenant_id",
                table: "shipment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_shipments_tenants_tenant_id",
                table: "shipments");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_allocations_tenants_tenant_id",
                table: "stock_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_count_lines_tenants_tenant_id",
                table: "stock_count_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_counts_tenants_tenant_id",
                table: "stock_counts");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_items_tenants_tenant_id",
                table: "stock_items");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_tenants_tenant_id",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_reason_codes_tenants_tenant_id",
                table: "stock_reason_codes");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transactions_tenants_tenant_id",
                table: "stock_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_order_items_tenants_tenant_id",
                table: "subscription_order_items");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_orders_tenants_tenant_id",
                table: "subscription_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_tags_tenants_tenant_id",
                table: "tags");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_declaration_lines_tenants_tenant_id",
                table: "tax_declaration_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_declarations_tenants_tenant_id",
                table: "tax_declarations");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_rates_tenants_tenant_id",
                table: "tax_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_identity_providers_tenants_tenant_id",
                table: "tenant_identity_providers");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_modules_tenants_tenant_id",
                table: "tenant_modules");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_provider_configs_tenants_tenant_id",
                table: "tenant_provider_configs");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_settings_store_tenants_tenant_id",
                table: "tenant_settings_store");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_theme_assets_tenants_tenant_id",
                table: "tenant_theme_assets");

            migrationBuilder.DropForeignKey(
                name: "fk_two_factor_backup_codes_tenants_tenant_id",
                table: "two_factor_backup_codes");

            migrationBuilder.DropForeignKey(
                name: "fk_two_factor_challenges_tenants_tenant_id",
                table: "two_factor_challenges");

            migrationBuilder.DropForeignKey(
                name: "fk_units_of_measure_tenants_tenant_id",
                table: "units_of_measure");

            migrationBuilder.DropForeignKey(
                name: "fk_user_consents_tenants_tenant_id",
                table: "user_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_user_device_tokens_tenants_tenant_id",
                table: "user_device_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_user_notification_preferences_tenants_tenant_id",
                table: "user_notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_addresses_tenants_tenant_id",
                table: "vendor_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_bank_accounts_tenants_tenant_id",
                table: "vendor_bank_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_bill_lines_tenants_tenant_id",
                table: "vendor_bill_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_bills_tenants_tenant_id",
                table: "vendor_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_contacts_tenants_tenant_id",
                table: "vendor_contacts");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_ledger_entries_tenants_tenant_id",
                table: "vendor_ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_payment_applications_tenants_tenant_id",
                table: "vendor_payment_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_payments_tenants_tenant_id",
                table: "vendor_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_vendors_tenants_tenant_id",
                table: "vendors");

            migrationBuilder.DropForeignKey(
                name: "fk_warehouses_tenants_tenant_id",
                table: "warehouses");

            migrationBuilder.DropForeignKey(
                name: "fk_warranty_contracts_tenants_tenant_id",
                table: "warranty_contracts");

            migrationBuilder.DropForeignKey(
                name: "fk_work_centers_tenants_tenant_id",
                table: "work_centers");

            migrationBuilder.DropIndex(
                name: "ix_vendor_bill_lines_tenant_id",
                table: "vendor_bill_lines");

            migrationBuilder.DropIndex(
                name: "ix_stock_count_lines_tenant_id",
                table: "stock_count_lines");

            migrationBuilder.DropIndex(
                name: "ix_shipment_lines_tenant_id",
                table: "shipment_lines");

            migrationBuilder.DropIndex(
                name: "ix_return_request_lines_tenant_id",
                table: "return_request_lines");

            migrationBuilder.DropIndex(
                name: "ix_quote_lines_tenant_id",
                table: "quote_lines");

            migrationBuilder.DropIndex(
                name: "ix_purchase_order_lines_tenant_id",
                table: "purchase_order_lines");

            migrationBuilder.DropIndex(
                name: "ix_project_template_reviews_tenant_id",
                table: "project_template_reviews");

            migrationBuilder.DropIndex(
                name: "ix_order_template_lines_tenant_id",
                table: "order_template_lines");

            migrationBuilder.DropIndex(
                name: "ix_invoice_lines_tenant_id",
                table: "invoice_lines");

            migrationBuilder.DropIndex(
                name: "ix_gl_posting_mappings_tenant_id",
                table: "gl_posting_mappings");

            migrationBuilder.DropIndex(
                name: "ix_feedback_tickets_tenant_id",
                table: "feedback_tickets");
        }
    }
}

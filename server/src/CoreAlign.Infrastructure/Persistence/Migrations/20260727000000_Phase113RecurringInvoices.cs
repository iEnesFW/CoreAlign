using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase113RecurringInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS recurring_invoice_templates (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    customer_id uuid NOT NULL,
    currency character varying(3) NOT NULL,
    frequency integer NOT NULL,
    interval_count integer NOT NULL DEFAULT 1,
    anchor_day_of_month integer NULL,
    anchor_day_of_week integer NULL,
    start_date date NOT NULL,
    end_date date NULL,
    max_occurrences integer NULL,
    next_run_date date NOT NULL,
    last_run_date date NULL,
    occurrences_generated integer NOT NULL DEFAULT 0,
    due_days integer NOT NULL DEFAULT 30,
    payment_terms_id uuid NULL,
    header_discount_percent numeric(6,3) NULL,
    header_discount_amount numeric(18,4) NULL,
    shipping_cost numeric(18,4) NULL,
    rounding_adjustment numeric(18,4) NULL,
    status integer NOT NULL DEFAULT 0,
    auto_confirm boolean NOT NULL DEFAULT true,
    public_notes text NULL,
    internal_notes text NULL,
    created_by_user_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_recurring_invoice_templates PRIMARY KEY (id),
    CONSTRAINT fk_recurring_invoice_templates_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT ck_rit_frequency CHECK (frequency IN (0,1,2,3)),
    CONSTRAINT ck_rit_status CHECK (status IN (0,1,2,3)),
    CONSTRAINT ck_rit_interval CHECK (interval_count >= 1),
    CONSTRAINT ck_rit_anchor_dom CHECK (anchor_day_of_month IS NULL OR anchor_day_of_month BETWEEN 1 AND 31),
    CONSTRAINT ck_rit_maxocc CHECK (max_occurrences IS NULL OR max_occurrences >= 1),
    CONSTRAINT ck_rit_occ_nonneg CHECK (occurrences_generated >= 0),
    CONSTRAINT ck_rit_due_days CHECK (due_days >= 0),
    CONSTRAINT ck_rit_hdr_pct CHECK (header_discount_percent IS NULL OR header_discount_percent BETWEEN 0 AND 100),
    CONSTRAINT ck_rit_window CHECK (end_date IS NULL OR end_date >= start_date)
);");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS recurring_invoice_template_lines (
    id uuid NOT NULL,
    template_id uuid NOT NULL,
    line_number integer NOT NULL,
    product_id uuid NULL,
    product_sku character varying(64) NOT NULL DEFAULT '',
    product_name character varying(200) NOT NULL DEFAULT '',
    description text NULL,
    quantity numeric(18,4) NOT NULL,
    unit_price numeric(18,4) NOT NULL,
    tax_rate_percent numeric(6,3) NOT NULL DEFAULT 0,
    tax_rate_id uuid NULL,
    line_discount_percent numeric(6,3) NULL,
    line_discount_amount numeric(18,4) NULL,
    withholding_rate_percent numeric(6,3) NULL,
    is_tax_inclusive boolean NOT NULL DEFAULT false,
    uom_id uuid NULL,
    uom_code character varying(16) NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_recurring_invoice_template_lines PRIMARY KEY (id),
    CONSTRAINT fk_ritl_template FOREIGN KEY (template_id) REFERENCES recurring_invoice_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_ritl_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT ck_ritl_qty CHECK (quantity > 0),
    CONSTRAINT ck_ritl_price CHECK (unit_price >= 0),
    CONSTRAINT ck_ritl_tax CHECK (tax_rate_percent BETWEEN 0 AND 100)
);");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS recurring_invoice_occurrences (
    id uuid NOT NULL,
    template_id uuid NOT NULL,
    period_key date NOT NULL,
    generated_invoice_id uuid NOT NULL,
    generated_at_utc timestamp with time zone NOT NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_recurring_invoice_occurrences PRIMARY KEY (id),
    CONSTRAINT fk_rio_template FOREIGN KEY (template_id) REFERENCES recurring_invoice_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_rio_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_rio_invoices FOREIGN KEY (generated_invoice_id) REFERENCES invoices (id) ON DELETE RESTRICT
);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_rit_tenant_customer ON recurring_invoice_templates (tenant_id, customer_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_rit_due ON recurring_invoice_templates (next_run_date) WHERE status = 0;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_ritl_template ON recurring_invoice_template_lines (template_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_ritl_tenant_template ON recurring_invoice_template_lines (tenant_id, template_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_rio_invoice ON recurring_invoice_occurrences (generated_invoice_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_rio_template ON recurring_invoice_occurrences (template_id);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_rio_tenant_template_period ON recurring_invoice_occurrences (tenant_id, template_id, period_key);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS recurring_invoice_occurrences;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS recurring_invoice_template_lines;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS recurring_invoice_templates;");
        }
    }
}

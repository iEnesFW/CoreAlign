using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase92Payroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_ytd_tax_bases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    cumulative_income_tax_base = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_min_wage_base = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    last_period_month = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_ytd_tax_bases", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_ytd_tax_bases_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    national_id = table.Column<string>(type: "char(11)", nullable: false),
                    sgk_registration_no = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    termination_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    employment_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    salary_basis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    base_salary_gross = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    salary_currency = table.Column<string>(type: "char(3)", nullable: false),
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    is_sgk_incentive_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    disability_degree = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_retired_working = table.Column<bool>(type: "boolean", nullable: false),
                    sgk_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    dependent_count = table.Column<int>(type: "integer", nullable: false),
                    spouse_employed = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    termination_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employees_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "payroll_parameters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_year = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sgk_employee_rate = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    sgk_employer_rate = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    sgk_employer5point_incentive_rate = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    unemployment_employee_rate = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    unemployment_employer_rate = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    sgk_floor_monthly = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sgk_ceiling_multiplier = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    sgk_ceiling_monthly = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    stamp_tax_rate = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    gross_minimum_wage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    min_wage_exemption_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    disability1amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    disability2amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    disability3amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_parameters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_deductions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deduction_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    percent = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    remaining_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_deductions", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_deductions_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_deductions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "salary_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    tax_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    sgk_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_salary_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_salary_components_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_salary_components_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    period_year = table.Column<int>(type: "integer", nullable: false),
                    period_month = table.Column<int>(type: "integer", nullable: false),
                    run_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    parameters_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_gross = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_sgk_employee = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_sgk_employer = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_unemployment_employee = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_unemployment_employer = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_income_tax = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_stamp_tax = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_deductions = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_net = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_employer_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    payslip_count = table.Column<int>(type: "integer", nullable: false),
                    calculated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_runs_payroll_parameters_parameters_id",
                        column: x => x.parameters_id,
                        principalTable: "payroll_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payroll_runs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_tax_brackets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_parameters_id = table.Column<Guid>(type: "uuid", nullable: false),
                    upper_bound = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    rate_percent = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_tax_brackets", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_tax_brackets_payroll_parameters_payroll_parameters_~",
                        column: x => x.payroll_parameters_id,
                        principalTable: "payroll_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payslips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payslip_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    employee_full_name = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    national_id = table.Column<string>(type: "char(11)", nullable: false),
                    period_year = table.Column<int>(type: "integer", nullable: false),
                    period_month = table.Column<int>(type: "integer", nullable: false),
                    days_worked = table.Column<int>(type: "integer", nullable: false),
                    parameters_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_earnings = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sgk_base = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    income_tax_base_this_period = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_income_tax_base_before = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_income_tax_base_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_min_wage_base_before = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_min_wage_base_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sgk_employee = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unemployment_employee = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    income_tax_gross = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    min_wage_income_tax_exemption_applied = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    min_wage_stamp_tax_exemption_applied = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    disability_exemption_applied = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    income_tax_net = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    stamp_tax = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    other_deductions_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_pay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sgk_employer = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unemployment_employer = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    employer_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payslips", x => x.id);
                    table.ForeignKey(
                        name: "fk_payslips_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payslips_payroll_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "payroll_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payslips_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payslip_deduction_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payslip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deduction_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payslip_deduction_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_payslip_deduction_lines_payslips_payslip_id",
                        column: x => x.payslip_id,
                        principalTable: "payslips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payslip_deduction_lines_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payslip_earning_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payslip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    sgk_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payslip_earning_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_payslip_earning_lines_payslips_payslip_id",
                        column: x => x.payslip_id,
                        principalTable: "payslips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payslip_earning_lines_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_deductions_employee_id",
                table: "employee_deductions",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_deductions_tenant_id_employee_id",
                table: "employee_deductions",
                columns: new[] { "tenant_id", "employee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_ytd_tax_bases_tenant_id_employee_id_year",
                table: "employee_ytd_tax_bases",
                columns: new[] { "tenant_id", "employee_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_employee_number",
                table: "employees",
                columns: new[] { "tenant_id", "employee_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_national_id",
                table: "employees",
                columns: new[] { "tenant_id", "national_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_status",
                table: "employees",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_user_id",
                table: "employees",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_parameters_tenant_id_effective_year",
                table: "payroll_parameters",
                columns: new[] { "tenant_id", "effective_year" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_parameters_tenant_id_is_active",
                table: "payroll_parameters",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_parameters_id",
                table: "payroll_runs",
                column: "parameters_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id_period_year_period_month_run_type",
                table: "payroll_runs",
                columns: new[] { "tenant_id", "period_year", "period_month", "run_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id_status_created_at_utc",
                table: "payroll_runs",
                columns: new[] { "tenant_id", "status", "created_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_tax_brackets_payroll_parameters_id",
                table: "payroll_tax_brackets",
                column: "payroll_parameters_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_tax_brackets_tenant_id_payroll_parameters_id_sort_o~",
                table: "payroll_tax_brackets",
                columns: new[] { "tenant_id", "payroll_parameters_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_payslip_deduction_lines_payslip_id",
                table: "payslip_deduction_lines",
                column: "payslip_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslip_deduction_lines_tenant_id",
                table: "payslip_deduction_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslip_earning_lines_payslip_id",
                table: "payslip_earning_lines",
                column: "payslip_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslip_earning_lines_tenant_id",
                table: "payslip_earning_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslips_employee_id",
                table: "payslips",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslips_run_id",
                table: "payslips",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslips_tenant_id_employee_id_period_year_period_month",
                table: "payslips",
                columns: new[] { "tenant_id", "employee_id", "period_year", "period_month" });

            migrationBuilder.CreateIndex(
                name: "ix_payslips_tenant_id_run_id",
                table: "payslips",
                columns: new[] { "tenant_id", "run_id" });

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_employee_id",
                table: "salary_components",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_tenant_id_employee_id",
                table: "salary_components",
                columns: new[] { "tenant_id", "employee_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_deductions");

            migrationBuilder.DropTable(
                name: "employee_ytd_tax_bases");

            migrationBuilder.DropTable(
                name: "payroll_tax_brackets");

            migrationBuilder.DropTable(
                name: "payslip_deduction_lines");

            migrationBuilder.DropTable(
                name: "payslip_earning_lines");

            migrationBuilder.DropTable(
                name: "salary_components");

            migrationBuilder.DropTable(
                name: "payslips");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "payroll_runs");

            migrationBuilder.DropTable(
                name: "payroll_parameters");
        }
    }
}

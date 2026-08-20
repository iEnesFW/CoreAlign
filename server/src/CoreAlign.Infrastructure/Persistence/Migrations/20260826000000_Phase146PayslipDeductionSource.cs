using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase146PayslipDeductionSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE payslip_deduction_lines
    ADD COLUMN IF NOT EXISTS employee_deduction_id uuid NULL;");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ix_payslip_deduction_lines_employee_deduction_id
    ON payslip_deduction_lines (employee_deduction_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_payslip_deduction_lines_employee_deduction_id;");

            migrationBuilder.Sql(@"
ALTER TABLE payslip_deduction_lines DROP COLUMN IF EXISTS employee_deduction_id;");
        }
    }
}

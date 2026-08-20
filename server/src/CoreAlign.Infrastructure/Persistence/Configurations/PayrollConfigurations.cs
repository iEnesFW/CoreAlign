using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(e => e.EmployeeNumber).HasMaxLength(32).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.NationalId).HasColumnType("char(11)").IsRequired();
        builder.Property(e => e.SgkRegistrationNo).HasColumnType("text");
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(32);
        builder.Property(e => e.HireDate).HasColumnType("date");
        builder.Property(e => e.TerminationDate).HasColumnType("date");
        builder.Property(e => e.Status).HasMaxLength(24).HasConversion<string>();
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.EmploymentType).HasMaxLength(24).HasConversion<string>();
        builder.Property(e => e.SalaryBasis).HasMaxLength(24).HasConversion<string>();
        builder.Property(e => e.BaseSalaryGross).HasColumnType("numeric(18,4)");
        builder.Property(e => e.SalaryCurrency).HasColumnType("char(3)").IsRequired();
        builder.Property(e => e.Iban).HasColumnType("text");
        builder.Property(e => e.BankName).HasMaxLength(150);
        builder.Property(e => e.DisabilityDegree).HasMaxLength(24).HasConversion<string>();
        builder.Property(e => e.TerminationReason).HasMaxLength(500);
        builder.Property(e => e.DeletedReason).HasMaxLength(500);
        builder.Property(e => e.DeletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.SalaryComponents).WithOne(c => c.Employee).HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Deductions).WithOne(d => d.Employee).HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeNumber }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.NationalId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(e => new { e.TenantId, e.Status });

        builder.Ignore(e => e.FullName);
    }
}

public class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ComponentType).HasMaxLength(24).HasConversion<string>();
        builder.Property(c => c.Amount).HasColumnType("numeric(18,4)");
        builder.Property(c => c.EffectiveFrom).HasColumnType("date");
        builder.Property(c => c.EffectiveTo).HasColumnType("date");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(c => new { c.TenantId, c.EmployeeId });
    }
}

public class EmployeeDeductionConfiguration : IEntityTypeConfiguration<EmployeeDeduction>
{
    public void Configure(EntityTypeBuilder<EmployeeDeduction> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DeductionType).HasMaxLength(24).HasConversion<string>();
        builder.Property(d => d.Amount).HasColumnType("numeric(18,4)");
        builder.Property(d => d.Percent).HasColumnType("numeric(6,4)");
        builder.Property(d => d.RemainingBalance).HasColumnType("numeric(18,4)");
        builder.Property(d => d.EffectiveFrom).HasColumnType("date");
        builder.Property(d => d.EffectiveTo).HasColumnType("date");
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(d => new { d.TenantId, d.EmployeeId });
    }
}

public class PayrollParametersConfiguration : IEntityTypeConfiguration<PayrollParameters>
{
    public void Configure(EntityTypeBuilder<PayrollParameters> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.EffectiveFrom).HasColumnType("date");
        builder.Property(p => p.EffectiveTo).HasColumnType("date");
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.SgkEmployeeRate).HasColumnType("numeric(6,5)");
        builder.Property(p => p.SgkEmployerRate).HasColumnType("numeric(6,5)");
        builder.Property(p => p.SgkEmployer5PointIncentiveRate).HasColumnType("numeric(6,5)");
        builder.Property(p => p.UnemploymentEmployeeRate).HasColumnType("numeric(6,5)");
        builder.Property(p => p.UnemploymentEmployerRate).HasColumnType("numeric(6,5)");
        builder.Property(p => p.SgkFloorMonthly).HasColumnType("numeric(18,4)");
        builder.Property(p => p.SgkCeilingMultiplier).HasColumnType("numeric(6,4)");
        builder.Property(p => p.SgkCeilingMonthly).HasColumnType("numeric(18,4)");
        builder.Property(p => p.StampTaxRate).HasColumnType("numeric(6,5)");
        builder.Property(p => p.GrossMinimumWage).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Disability1Amount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Disability2Amount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Disability3Amount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(p => p.TaxBrackets).WithOne(b => b.Parameters).HasForeignKey(b => b.PayrollParametersId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.EffectiveYear });
        builder.HasIndex(p => new { p.TenantId, p.IsActive });
    }
}

public class PayrollTaxBracketConfiguration : IEntityTypeConfiguration<PayrollTaxBracket>
{
    public void Configure(EntityTypeBuilder<PayrollTaxBracket> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.UpperBound).HasColumnType("numeric(18,4)");
        builder.Property(b => b.RatePercent).HasColumnType("numeric(6,4)");
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(b => new { b.TenantId, b.PayrollParametersId, b.SortOrder });
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(r => r.RunNumber).HasMaxLength(64).IsRequired();
        builder.Property(r => r.RunType).HasMaxLength(24).HasConversion<string>();
        builder.Property(r => r.Status).HasMaxLength(24).HasConversion<string>();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(r => r.TotalGross).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalSgkEmployee).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalSgkEmployer).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalUnemploymentEmployee).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalUnemploymentEmployer).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalIncomeTax).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalStampTax).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalDeductions).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalNet).HasColumnType("numeric(18,4)");
        builder.Property(r => r.TotalEmployerCost).HasColumnType("numeric(18,4)");
        builder.Property(r => r.CalculatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.ApprovedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.PostedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.PaidAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(r => r.Parameters).WithMany().HasForeignKey(r => r.ParametersId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.TenantId, r.PeriodYear, r.PeriodMonth, r.RunType }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.Status, r.CreatedAtUtc }).IsDescending(false, false, true);
    }
}

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(p => p.PayslipNumber).HasMaxLength(64).IsRequired();
        builder.Property(p => p.EmployeeNumber).HasMaxLength(32).IsRequired();
        builder.Property(p => p.EmployeeFullName).HasMaxLength(201).IsRequired();
        builder.Property(p => p.NationalId).HasColumnType("text").IsRequired();
        builder.Property(p => p.GrossEarnings).HasColumnType("numeric(18,4)");
        builder.Property(p => p.SgkBase).HasColumnType("numeric(18,4)");
        builder.Property(p => p.IncomeTaxBaseThisPeriod).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CumulativeIncomeTaxBaseBefore).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CumulativeIncomeTaxBaseAfter).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CumulativeMinWageBaseBefore).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CumulativeMinWageBaseAfter).HasColumnType("numeric(18,4)");
        builder.Property(p => p.SgkEmployee).HasColumnType("numeric(18,4)");
        builder.Property(p => p.UnemploymentEmployee).HasColumnType("numeric(18,4)");
        builder.Property(p => p.IncomeTaxGross).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MinWageIncomeTaxExemptionApplied).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MinWageStampTaxExemptionApplied).HasColumnType("numeric(18,4)");
        builder.Property(p => p.DisabilityExemptionApplied).HasColumnType("numeric(18,4)");
        builder.Property(p => p.IncomeTaxNet).HasColumnType("numeric(18,4)");
        builder.Property(p => p.StampTax).HasColumnType("numeric(18,4)");
        builder.Property(p => p.OtherDeductionsTotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.NetPay).HasColumnType("numeric(18,4)");
        builder.Property(p => p.SgkEmployer).HasColumnType("numeric(18,4)");
        builder.Property(p => p.UnemploymentEmployer).HasColumnType("numeric(18,4)");
        builder.Property(p => p.EmployerCost).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Run).WithMany().HasForeignKey(p => p.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Employee).WithMany().HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.EarningLines).WithOne(l => l.Payslip).HasForeignKey(l => l.PayslipId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.DeductionLines).WithOne(l => l.Payslip).HasForeignKey(l => l.PayslipId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.RunId });
        builder.HasIndex(p => new { p.TenantId, p.EmployeeId, p.PeriodYear, p.PeriodMonth });
    }
}

public class PayslipEarningLineConfiguration : IEntityTypeConfiguration<PayslipEarningLine>
{
    public void Configure(EntityTypeBuilder<PayslipEarningLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ComponentType).HasMaxLength(24).HasConversion<string>();
        builder.Property(l => l.Amount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => l.PayslipId);
    }
}

public class PayslipDeductionLineConfiguration : IEntityTypeConfiguration<PayslipDeductionLine>
{
    public void Configure(EntityTypeBuilder<PayslipDeductionLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.DeductionType).HasMaxLength(24).HasConversion<string>();
        builder.Property(l => l.Amount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => l.PayslipId);
        // No FK: the deduction row can be deleted while the payslip that withheld against it must
        // stay immutable, so the link is a soft reference (the payslip amount is the record).
        builder.HasIndex(l => l.EmployeeDeductionId);
    }
}

public class EmployeeYtdTaxBaseConfiguration : IEntityTypeConfiguration<EmployeeYtdTaxBase>
{
    public void Configure(EntityTypeBuilder<EmployeeYtdTaxBase> builder)
    {
        builder.HasKey(y => y.Id);
        builder.Property(y => y.CumulativeIncomeTaxBase).HasColumnType("numeric(18,4)");
        builder.Property(y => y.CumulativeMinWageBase).HasColumnType("numeric(18,4)");
        builder.Property(y => y.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(y => y.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(y => new { y.TenantId, y.EmployeeId, y.Year }).IsUnique();
    }
}

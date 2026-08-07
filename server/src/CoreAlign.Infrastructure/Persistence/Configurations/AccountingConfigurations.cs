using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.StartDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.EndDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ClosedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ReopenedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.Year, p.Month }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.Ignore(p => p.IsClosed);
    }
}

public class GLAccountConfiguration : IEntityTypeConfiguration<GLAccount>
{
    public void Configure(EntityTypeBuilder<GLAccount> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Code).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.NormalSide).HasConversion<string>().HasMaxLength(8);
        builder.Property(a => a.Currency).HasMaxLength(3).IsRequired();
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        // Self-referential parent — Restrict on delete so a child can't accidentally
        // lose its anchor in the hierarchy; the application layer enforces "no
        // delete with children" explicitly.
        builder.HasOne(a => a.Parent)
            .WithMany()
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.Code }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.ParentId });
        builder.HasIndex(a => new { a.TenantId, a.Type, a.IsActive });
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(j => j.Number).HasMaxLength(32).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(1000);
        builder.Property(j => j.Reference).HasMaxLength(200);
        builder.Property(j => j.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(16);
        // WHY: the column has always been varchar(32); without this conversion EF binds the enum as
        // an integer and Postgres rejects every read AND write with 42883 (varchar = integer).
        builder.Property(j => j.SourceType).HasConversion<string>().HasMaxLength(32);
        builder.Property(j => j.TotalDebit).HasColumnType("numeric(18,4)");
        builder.Property(j => j.TotalCredit).HasColumnType("numeric(18,4)");
        builder.Property(j => j.EntryDate).HasColumnType("timestamp with time zone");
        builder.Property(j => j.PostingDate).HasColumnType("timestamp with time zone");
        builder.Property(j => j.PostedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.ReversedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(j => j.Lines)
            .WithOne()
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(j => new { j.TenantId, j.Number }).IsUnique();
        builder.HasIndex(j => new { j.TenantId, j.PostingDate });
        builder.HasIndex(j => new { j.TenantId, j.Status, j.PostingDate });
        builder.HasIndex(j => new { j.TenantId, j.Type });
    }
}

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.AccountCode).HasMaxLength(32).IsRequired();
        builder.Property(l => l.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.CostCenter).HasMaxLength(64);
        builder.Property(l => l.Project).HasMaxLength(64);
        builder.Property(l => l.Currency).HasMaxLength(3).IsRequired();
        builder.Property(l => l.Debit).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Credit).HasColumnType("numeric(18,4)");
        builder.Property(l => l.ForeignAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<GLAccount>().WithMany().HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.TenantId, l.AccountId });
        builder.HasIndex(l => new { l.TenantId, l.JournalEntryId, l.LineNumber }).IsUnique();
    }
}

public class CustomerProductPriceConfiguration : IEntityTypeConfiguration<CustomerProductPrice>
{
    public void Configure(EntityTypeBuilder<CustomerProductPrice> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Price).HasColumnType("numeric(18,4)");
        builder.Property(p => p.DiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(p => p.MinQuantity).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MaxQuantity).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.ValidFromUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.CustomerId, p.ProductId });
        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.IsActive });
    }
}

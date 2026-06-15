using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class CustomerUserConfiguration : IEntityTypeConfiguration<CustomerUser>
{
    public void Configure(EntityTypeBuilder<CustomerUser> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.InvitedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.AcceptedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.LastLoginAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(c => c.MembershipRole).HasConversion<int>();
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.SuspensionReason).HasMaxLength(512);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.CustomerId });
        builder.HasIndex(c => new { c.TenantId, c.UserId });
        builder.HasIndex(c => new { c.TenantId, c.CustomerId, c.UserId }).IsUnique();
    }
}

public class DealerAccountConfiguration : IEntityTypeConfiguration<DealerAccount>
{
    public void Configure(EntityTypeBuilder<DealerAccount> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(d => d.Code).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.LegalName).HasMaxLength(200);
        builder.Property(d => d.TaxNumber).HasMaxLength(64);
        builder.Property(d => d.Email).HasMaxLength(256);
        builder.Property(d => d.Phone).HasMaxLength(64);
        builder.Property(d => d.Address).HasMaxLength(512);
        builder.Property(d => d.Notes).HasMaxLength(2000);
        builder.Property(d => d.SuspensionReason).HasMaxLength(512);
        builder.Property(d => d.Status).HasConversion<int>();
        builder.Property(d => d.CommissionPercent)
            .HasColumnType("numeric(7,4)")
            .HasDefaultValue(0m);

        builder.HasIndex(d => new { d.TenantId, d.Code }).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.Status });
    }
}

public class DealerUserConfiguration : IEntityTypeConfiguration<DealerUser>
{
    public void Configure(EntityTypeBuilder<DealerUser> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.InvitedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.AcceptedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.LastLoginAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(d => d.MembershipRole).HasConversion<int>();
        builder.Property(d => d.Status).HasConversion<int>();
        builder.Property(d => d.SuspensionReason).HasMaxLength(512);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DealerAccount)
            .WithMany()
            .HasForeignKey(d => d.DealerAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.TenantId, d.DealerAccountId });
        builder.HasIndex(d => new { d.TenantId, d.UserId });
        builder.HasIndex(d => new { d.TenantId, d.DealerAccountId, d.UserId }).IsUnique();
    }
}

public class DealerCustomerLinkConfiguration : IEntityTypeConfiguration<DealerCustomerLink>
{
    public void Configure(EntityTypeBuilder<DealerCustomerLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.AssignedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.RevokedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(l => l.Status).HasConversion<int>();
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.RevokeReason).HasMaxLength(512);
        builder.Property(l => l.CommissionPercentOverride)
            .HasColumnType("numeric(7,4)");

        builder.HasOne(l => l.DealerAccount)
            .WithMany()
            .HasForeignKey(l => l.DealerAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Customer)
            .WithMany()
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.TenantId, l.DealerAccountId });
        builder.HasIndex(l => new { l.TenantId, l.CustomerId });
        builder.HasIndex(l => new { l.TenantId, l.DealerAccountId, l.CustomerId }).IsUnique();
    }
}

public class DealerCommissionLedgerEntryConfiguration : IEntityTypeConfiguration<DealerCommissionLedgerEntry>
{
    public void Configure(EntityTypeBuilder<DealerCommissionLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.AccruedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.PaidOutAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(e => e.Currency).HasMaxLength(8).IsRequired();
        builder.Property(e => e.OrderTotal).HasColumnType("numeric(18,4)");
        builder.Property(e => e.CommissionPercent).HasColumnType("numeric(7,4)");
        builder.Property(e => e.CommissionAmount).HasColumnType("numeric(18,4)");
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => new { e.TenantId, e.DealerAccountId, e.AccruedAtUtc })
            .HasDatabaseName("IX_DealerCommissionLedgerEntries_Tenant_Dealer_AccruedAtUtc");
        builder.HasIndex(e => new { e.TenantId, e.DealerAccountId, e.Status })
            .HasDatabaseName("IX_DealerCommissionLedgerEntries_Tenant_Dealer_Status");
        builder.HasIndex(e => new { e.TenantId, e.DealerAccountId, e.OrderId, e.ShipmentId })
            .IsUnique()
            .HasDatabaseName("UX_DealerCommissionLedgerEntries_Tenant_Dealer_Order_Shipment");
    }
}

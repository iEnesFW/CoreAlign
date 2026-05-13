using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.Label).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.State).HasMaxLength(100);
        builder.Property(a => a.PostalCode).HasMaxLength(32);
        builder.Property(a => a.Country).HasMaxLength(100);

        builder.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TenantId, a.CustomerId });
        builder.HasIndex(a => new { a.TenantId, a.CustomerId })
            .IsUnique()
            .HasFilter("is_primary = true")
            .HasDatabaseName("ix_customer_addresses_primary_unique");
    }
}

public class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Role).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.CustomerId });
        builder.HasIndex(c => new { c.TenantId, c.CustomerId })
            .IsUnique()
            .HasFilter("is_primary = true")
            .HasDatabaseName("ix_customer_contacts_primary_unique");
    }
}

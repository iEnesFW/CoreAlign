using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class CustomerDealerProductVisibilityConfiguration : IEntityTypeConfiguration<CustomerDealerProductVisibility>
{
    public void Configure(EntityTypeBuilder<CustomerDealerProductVisibility> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(v => v.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(v => v.DealerCustomerLink)
            .WithMany()
            .HasForeignKey(v => v.DealerCustomerLinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Product)
            .WithMany()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.TenantId, v.DealerCustomerLinkId })
            .HasDatabaseName("ix_cdpv_tenant_link");
        builder.HasIndex(v => new { v.TenantId, v.DealerCustomerLinkId, v.ProductId })
            .IsUnique()
            .HasDatabaseName("ux_cdpv_tenant_link_product");
    }
}

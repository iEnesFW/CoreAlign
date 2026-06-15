using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.Name).HasMaxLength(64).IsRequired();
        builder.Property(t => t.ColorHex).HasMaxLength(9);

        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
    }
}

public class CustomerTagLinkConfiguration : IEntityTypeConfiguration<CustomerTagLink>
{
    public void Configure(EntityTypeBuilder<CustomerTagLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Customer).WithMany().HasForeignKey(l => l.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Tag).WithMany().HasForeignKey(l => l.TagId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.TenantId, l.CustomerId });
        builder.HasIndex(l => new { l.TenantId, l.CustomerId, l.TagId }).IsUnique();
    }
}

using CoreAlign.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class OrderTemplateConfiguration : IEntityTypeConfiguration<OrderTemplate>
{
    public void Configure(EntityTypeBuilder<OrderTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(2000);
        builder.Property(t => t.Frequency).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.NextRunAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.LastRunAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(t => t.Lines)
            .WithOne(l => l.OrderTemplate!)
            .HasForeignKey(l => l.OrderTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TenantId, t.CustomerId });
        builder.HasIndex(t => new { t.TenantId, t.IsActive, t.NextRunAtUtc });
    }
}

public class OrderTemplateLineConfiguration : IEntityTypeConfiguration<OrderTemplateLine>
{
    public void Configure(EntityTypeBuilder<OrderTemplateLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => l.OrderTemplateId);
        builder.HasIndex(l => l.ProductId);
    }
}

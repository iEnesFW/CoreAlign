using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class WithholdingTaxCodeConfiguration : IEntityTypeConfiguration<WithholdingTaxCode>
{
    public void Configure(EntityTypeBuilder<WithholdingTaxCode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.ValidFrom).HasColumnType("date");
        builder.Property(x => x.ValidTo).HasColumnType("date");
        builder.Ignore(x => x.Fraction);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public class VatExemptionCodeConfiguration : IEntityTypeConfiguration<VatExemptionCode>
{
    public void Configure(EntityTypeBuilder<VatExemptionCode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LawReference).HasMaxLength(100);
        builder.Property(x => x.Kind).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

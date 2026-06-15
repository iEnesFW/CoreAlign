using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TaxDeclarationConfiguration : IEntityTypeConfiguration<TaxDeclaration>
{
    public void Configure(EntityTypeBuilder<TaxDeclaration> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Year).IsRequired();
        builder.Property(d => d.Month).IsRequired();
        builder.Property(d => d.DeclarationType).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(d => d.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(d => d.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(d => d.TotalAmount).HasColumnType("numeric(18,4)");
        builder.Property(d => d.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(d => d.WithholdingAmount).HasColumnType("numeric(18,4)");
        builder.Property(d => d.XmlPayload).HasColumnType("text");
        builder.Property(d => d.LineCount);
        builder.Property(d => d.FailureReason).HasMaxLength(500);
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.GeneratedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.AcceptedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(d => d.Lines)
            .WithOne(l => l.TaxDeclaration)
            .HasForeignKey(l => l.TaxDeclarationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.TenantId, d.Year, d.Month, d.DeclarationType }).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.Status });
    }
}

public class TaxDeclarationLineConfiguration : IEntityTypeConfiguration<TaxDeclarationLine>
{
    public void Configure(EntityTypeBuilder<TaxDeclarationLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.CounterpartyTaxNumber).HasMaxLength(20);
        builder.Property(l => l.CounterpartyName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.DocumentCount);
        builder.Property(l => l.TotalAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => new { l.TenantId, l.TaxDeclarationId });
    }
}

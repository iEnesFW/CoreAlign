using CoreAlign.Domain.Entities.Mrp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Mrp;

public class MrpPeggingConfiguration : IEntityTypeConfiguration<MrpPegging>
{
    public void Configure(EntityTypeBuilder<MrpPegging> builder)
    {
        builder.HasKey(p => p.Id);
        builder.ToTable("mrp_peggings");
        builder.Property(p => p.RequirementQuantity).HasColumnType("numeric(18,4)");
        builder.Property(p => p.SourceKind).HasMaxLength(30).IsRequired();
        builder.Property(p => p.DueDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.PlanRunId, p.ComponentProductId });
    }
}

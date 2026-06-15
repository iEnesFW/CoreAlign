using CoreAlign.Domain.Entities.Mrp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Mrp;

public class MrpActionMessageConfiguration : IEntityTypeConfiguration<MrpActionMessage>
{
    public void Configure(EntityTypeBuilder<MrpActionMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.ToTable("mrp_action_messages");
        builder.Property(m => m.ActionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(m => m.Message).HasMaxLength(500);
        builder.Property(m => m.CurrentDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.SuggestedDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.DismissedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(m => new { m.TenantId, m.PlanRunId, m.ActionType });
        builder.HasIndex(m => new { m.TenantId, m.IsDismissed });
    }
}

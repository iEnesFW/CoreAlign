using System.Text.Json;
using CoreAlign.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class OrderRevisionConfiguration : IEntityTypeConfiguration<OrderRevision>
{
    public void Configure(EntityTypeBuilder<OrderRevision> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.OrderId).IsRequired();
        builder.Property(r => r.RevisionNumber).IsRequired();
        builder.Property(r => r.RequestedByUserId).IsRequired();
        builder.Property(r => r.RequestedByPersona).HasMaxLength(16).IsRequired();
        builder.Property(r => r.RequestedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.DecidedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Property(r => r.RequestNotes).HasMaxLength(1000);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        var jsonOpts = new JsonSerializerOptions();
        builder.Property(r => r.ProposedLines)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOpts),
                v => string.IsNullOrEmpty(v)
                    ? new List<RevisionLineSnapshot>()
                    : JsonSerializer.Deserialize<List<RevisionLineSnapshot>>(v, jsonOpts) ?? new List<RevisionLineSnapshot>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IList<RevisionLineSnapshot>>(
                (a, b) => ReferenceEquals(a, b),
                v => v == null ? 0 : v.Count,
                v => v));

        builder.HasIndex(r => new { r.TenantId, r.OrderId, r.RevisionNumber }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.OrderId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.Status, r.RequestedAtUtc });

        builder.ToTable("order_revisions");
        builder.Ignore(r => r.IsPending);
        builder.Ignore(r => r.IsTerminal);
    }
}

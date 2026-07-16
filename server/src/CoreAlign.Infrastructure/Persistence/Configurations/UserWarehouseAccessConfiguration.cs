using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class UserWarehouseAccessConfiguration : IEntityTypeConfiguration<UserWarehouseAccess>
{
    public void Configure(EntityTypeBuilder<UserWarehouseAccess> builder)
    {
        // DbSet-less entity (accessed via _context.Set<UserWarehouseAccess>()); explicit plural table name.
        // WHY: user_id / granted_by_user_id are FK-less soft Guids — User is tenant-filter-exempt (§4.4).
        builder.ToTable("user_warehouse_access");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(a => a.Warehouse).WithMany().HasForeignKey(a => a.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.UserId, a.WarehouseId }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.UserId });
    }
}

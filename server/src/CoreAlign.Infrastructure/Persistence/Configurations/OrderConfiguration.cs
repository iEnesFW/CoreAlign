using System.Text.Json;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(o => o.OrderNumber).HasMaxLength(64).IsRequired();
        builder.Property(o => o.Type).HasMaxLength(20).HasConversion<string>();
        builder.Property(o => o.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(o => o.Source).HasMaxLength(20).HasConversion<string>();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(o => o.Subtotal).HasColumnType("numeric(18,4)");
        builder.Property(o => o.LineDiscountTotal).HasColumnType("numeric(18,4)");
        builder.Property(o => o.HeaderDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(o => o.HeaderDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(o => o.TaxableTotal).HasColumnType("numeric(18,4)");
        builder.Property(o => o.TaxTotal).HasColumnType("numeric(18,4)");
        builder.Property(o => o.WithholdingTotal).HasColumnType("numeric(18,4)");
        builder.Property(o => o.ShippingCost).HasColumnType("numeric(18,4)");
        builder.Property(o => o.RoundingAdjustment).HasColumnType("numeric(18,4)");
        builder.Property(o => o.Total).HasColumnType("numeric(18,4)");
        // Phase59 migration original_submitted_snapshot_json sutununu jsonb olarak yaratti — entity mapping de jsonb olmali.
        builder.Property(o => o.OriginalSubmittedSnapshotJson).HasColumnType("jsonb");
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.InternalNotes).HasMaxLength(2000);
        builder.Property(o => o.CustomerNotes).HasMaxLength(2000);
        builder.Property(o => o.Channel).HasMaxLength(32);
        builder.Property(o => o.CancelReason).HasMaxLength(500);
        builder.Property(o => o.OrderDate).HasColumnType("timestamp with time zone");
        builder.Property(o => o.RequestedDeliveryDate).HasColumnType("timestamp with time zone");
        builder.Property(o => o.PromisedDeliveryDate).HasColumnType("timestamp with time zone");
        builder.Property(o => o.ActualDeliveryDate).HasColumnType("timestamp with time zone");
        builder.Property(o => o.DueDate).HasColumnType("timestamp with time zone");
        builder.Property(o => o.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.ApprovedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        var jsonOpts = new JsonSerializerOptions();
        builder.Property(o => o.CustomerSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<CustomerSnapshot>(v, jsonOpts));
        builder.Property(o => o.BillingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));
        builder.Property(o => o.ShippingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));

        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Shipments).WithOne(s => s.Order).HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.CustomerId });
        builder.HasIndex(o => new { o.TenantId, o.Status, o.OrderDate }).IsDescending(false, false, true);
        builder.HasIndex(o => new { o.TenantId, o.OrderDate }).IsDescending(false, true);
        builder.HasIndex(o => new { o.TenantId, o.DueDate });

        builder.Ignore(o => o.IsDraft);
        builder.Ignore(o => o.IsCancellable);
        builder.Ignore(o => o.IsEditable);
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.ProductDescriptionSnapshot).HasMaxLength(2000);
        builder.Property(l => l.UomCode).HasMaxLength(20);
        builder.Property(l => l.UomConversionFactor).HasColumnType("numeric(18,6)");
        builder.Property(l => l.WidthMm).HasColumnType("numeric(12,2)");
        builder.Property(l => l.HeightMm).HasColumnType("numeric(12,2)");
        builder.Property(l => l.Pieces).HasColumnType("numeric(12,2)");
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityAllocated).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityShipped).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityInvoiced).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityReturned).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityCancelled).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityScrapped).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(l => l.ListPriceSnapshot).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.LineDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.WithholdingRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.WithholdingAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.WithholdingCode).HasMaxLength(8);
        builder.Property(l => l.LineSubtotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineNetAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineTotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitCostSnapshot).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(l => l.LineNotes).HasMaxLength(1000);
        builder.Property(l => l.ScrapReason).HasMaxLength(500);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WithholdingTaxCode>().WithMany().HasForeignKey(l => l.WithholdingTaxCodeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.OrderId);
        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => new { l.TenantId, l.Status });
        builder.HasIndex(l => new { l.TenantId, l.SourceBomLineId });
        builder.HasIndex(l => new { l.TenantId, l.SubstituteFromProductId });
        builder.Ignore(l => l.LineTaxAmount);
        builder.Ignore(l => l.LineWithholdingAmount);
        builder.Ignore(l => l.QuantityRemainingToShip);
        builder.Ignore(l => l.QuantityRemainingToInvoice);
    }
}

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(s => s.ShipmentNumber).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(s => s.CarrierName).HasMaxLength(150);
        builder.Property(s => s.TrackingNumber).HasMaxLength(100);
        builder.Property(s => s.TrackingUrl).HasMaxLength(500);
        builder.Property(s => s.ShippingCost).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ReceivedBy).HasMaxLength(150);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CancelReason).HasMaxLength(500);
        builder.Property(s => s.CarrierVkn).HasMaxLength(11);
        builder.Property(s => s.VehiclePlate).HasMaxLength(20);
        builder.Property(s => s.DriverName).HasMaxLength(150);
        builder.Property(s => s.DriverTckn).HasMaxLength(11);
        builder.Property(s => s.EDespatchUuid).HasMaxLength(64);
        builder.Property(s => s.EDespatchStatus).HasMaxLength(20);
        builder.Property(s => s.EDespatchProfile).HasMaxLength(32);
        builder.Property(s => s.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(s => s.PickedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.PackedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.DispatchedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.DeliveredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        var jsonOpts = new JsonSerializerOptions();
        builder.Property(s => s.ShippingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));

        builder.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(s => s.Lines).WithOne(l => l.Shipment).HasForeignKey(l => l.ShipmentId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TenantId, s.ShipmentNumber }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.OrderId });
        builder.HasIndex(s => new { s.TenantId, s.Status, s.CreatedDate }).IsDescending(false, false, true);
        builder.HasIndex(s => new { s.TenantId, s.CustomerId });
    }
}

public class ShipmentLineConfiguration : IEntityTypeConfiguration<ShipmentLine>
{
    public void Configure(EntityTypeBuilder<ShipmentLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.SerialNumber).HasMaxLength(64);
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitCostSnapshot).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.OrderLine).WithMany().HasForeignKey(l => l.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Lot).WithMany().HasForeignKey(l => l.LotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.ShipmentId);
        builder.HasIndex(l => l.OrderLineId);
    }
}

using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Code).HasMaxLength(64).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(500);
        builder.Property(m => m.Category).HasMaxLength(64);
        builder.Property(m => m.IconKey).HasMaxLength(64);
        builder.Property(m => m.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(m => m.Code).IsUnique();
        builder.HasIndex(m => new { m.IsActive, m.SortOrder });

        builder.HasMany(m => m.PricePlans)
            .WithOne(p => p.Module)
            .HasForeignKey(p => p.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ModulePricePlanConfiguration : IEntityTypeConfiguration<ModulePricePlan>
{
    public void Configure(EntityTypeBuilder<ModulePricePlan> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.DisplayLabel).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Price).HasColumnType("numeric(18,4)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.ModuleId, p.Code }).IsUnique();
        builder.HasIndex(p => new { p.ModuleId, p.IsActive, p.SortOrder });
    }
}

public class TenantModuleConfiguration : IEntityTypeConfiguration<TenantModule>
{
    public void Configure(EntityTypeBuilder<TenantModule> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.StartUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.EndUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.Source).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => new { t.TenantId, t.ModuleId }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.EndUtc });
    }
}

public class SubscriptionOrderConfiguration : IEntityTypeConfiguration<SubscriptionOrder>
{
    public void Configure(EntityTypeBuilder<SubscriptionOrder> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(48).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.TotalAmount).HasColumnType("numeric(18,4)");
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.GatewayName).HasMaxLength(32);
        builder.Property(o => o.GatewayIntentId).HasMaxLength(128);
        builder.Property(o => o.PaymentReference).HasMaxLength(128);
        builder.Property(o => o.PaymentTransactionId).HasMaxLength(128);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.BuyerName).HasMaxLength(100);
        builder.Property(o => o.BuyerSurname).HasMaxLength(100);
        builder.Property(o => o.BuyerEmail).HasMaxLength(256);
        builder.Property(o => o.BuyerGsmNumber).HasMaxLength(32);
        builder.Property(o => o.BuyerIdentityNumber).HasMaxLength(32);
        builder.Property(o => o.BuyerIpAddress).HasMaxLength(64);
        builder.Property(o => o.BillingAddress).HasMaxLength(500);
        builder.Property(o => o.BillingCity).HasMaxLength(100);
        builder.Property(o => o.BillingCountry).HasMaxLength(100);
        builder.Property(o => o.BillingZipCode).HasMaxLength(32);
        builder.Property(o => o.PaidAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.Status, o.CreatedAtUtc });
        builder.HasIndex(o => new { o.GatewayName, o.GatewayIntentId })
            .HasFilter("gateway_intent_id IS NOT NULL")
            .HasDatabaseName("ix_subscription_orders_gateway_intent");

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.SubscriptionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Attempts)
            .WithOne(a => a.Order)
            .HasForeignKey(a => a.SubscriptionOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriptionOrderItemConfiguration : IEntityTypeConfiguration<SubscriptionOrderItem>
{
    public void Configure(EntityTypeBuilder<SubscriptionOrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ModuleCode).HasMaxLength(64).IsRequired();
        builder.Property(i => i.ModuleName).HasMaxLength(150).IsRequired();
        builder.Property(i => i.PlanLabel).HasMaxLength(150).IsRequired();
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(i => new { i.TenantId, i.SubscriptionOrderId });
    }
}

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.GatewayName).HasMaxLength(32).IsRequired();
        builder.Property(a => a.IntentId).HasMaxLength(128);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.Amount).HasColumnType("numeric(18,4)");
        builder.Property(a => a.Currency).HasMaxLength(3).IsRequired();
        builder.Property(a => a.RawResponseJson).HasColumnType("jsonb");
        builder.Property(a => a.FailureReason).HasMaxLength(500);
        builder.Property(a => a.AttemptedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(a => new { a.TenantId, a.SubscriptionOrderId });
    }
}

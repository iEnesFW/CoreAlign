using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(80).IsRequired();
        builder.Property(t => t.LegalName).HasMaxLength(200);
        builder.Property(t => t.TradeName).HasMaxLength(200);
        builder.Property(t => t.TaxNumber).HasMaxLength(50);
        builder.Property(t => t.TaxOffice).HasMaxLength(100);
        builder.Property(t => t.NationalId).HasMaxLength(32);
        builder.Property(t => t.MersisNumber).HasMaxLength(32);
        builder.Property(t => t.TradeRegistryNumber).HasMaxLength(64);
        builder.Property(t => t.Sector).HasMaxLength(100);
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
        builder.Property(t => t.AddressLine1).HasMaxLength(200);
        builder.Property(t => t.AddressLine2).HasMaxLength(200);
        builder.Property(t => t.City).HasMaxLength(100);
        builder.Property(t => t.StateProvince).HasMaxLength(100);
        builder.Property(t => t.PostalCode).HasMaxLength(20);
        builder.Property(t => t.Country).HasMaxLength(100);
        builder.Property(t => t.Phone).HasMaxLength(30);
        builder.Property(t => t.Fax).HasMaxLength(30);
        builder.Property(t => t.Email).HasMaxLength(256);
        builder.Property(t => t.Website).HasMaxLength(500);
        builder.Property(t => t.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.ReportingCurrency).HasMaxLength(3);
        builder.Property(t => t.LocaleCode).HasMaxLength(10).IsRequired();
        builder.Property(t => t.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(t => t.PrimaryColor).HasMaxLength(16);
        builder.Property(t => t.SecondaryColor).HasMaxLength(16);
        builder.Property(t => t.FoundedOn).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}

public class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Category).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Key).HasMaxLength(128).IsRequired();
        builder.Property(s => s.DataType).HasMaxLength(16).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => new { s.TenantId, s.Category });
        builder.HasIndex(s => new { s.TenantId, s.Category, s.Key }).IsUnique();
    }
}

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Body).IsRequired();
        builder.Property(t => t.Locale).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => new { t.TenantId, t.Code, t.Locale }).IsUnique();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(64).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.SecurityStamp).HasMaxLength(128).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(64);
        builder.Property(u => u.LastName).HasMaxLength(64);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.TwoFactorSecretKey).HasColumnType("text");
        builder.Property(u => u.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(u => u.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(u => u.LastLoginAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(u => u.LockoutEnd).HasColumnType("timestamp with time zone");

        builder.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.Username }).IsUnique();
        builder.HasIndex(u => u.NormalizedEmail);

        builder.Ignore(u => u.IsLockedOut);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityByDefaultColumn();
        builder.Property(r => r.Name).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(256);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new { Id = 1, Name = "TenantAdmin", Description = (string?)"Tenant administrator with full access.", IsActive = true, CreatedAtUtc = SeedDate },
            new { Id = 2, Name = "User", Description = (string?)"Standard user.", IsActive = true, CreatedAtUtc = SeedDate });
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Property(ur => ur.AssignedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.AssignedByUser).WithMany().HasForeignKey(ur => ur.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(t => t.DeviceInfo).HasMaxLength(512);
        builder.Property(t => t.IpAddress).HasMaxLength(45);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(512);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.RevokedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsActive);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UsedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsValid);
    }
}

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UsedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsValid);
    }
}

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityByDefaultColumn();
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PriceMonthly).HasColumnType("numeric(10,2)");
        builder.Property(p => p.PriceYearly).HasColumnType("numeric(10,2)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(
            new { Id = 1, Name = "FreeTrial", DisplayName = "Free Trial", MaxUsers = 3, MaxProjects = 5, PriceMonthly = 0m, PriceYearly = 0m, TrialDurationDays = 14, IsActive = true, CreatedAtUtc = SeedDate },
            new { Id = 2, Name = "Standard", DisplayName = "Standard", MaxUsers = 10, MaxProjects = 50, PriceMonthly = 29m, PriceYearly = 290m, TrialDurationDays = 0, IsActive = true, CreatedAtUtc = SeedDate },
            new { Id = 3, Name = "Pro", DisplayName = "Professional", MaxUsers = 50, MaxProjects = 500, PriceMonthly = 99m, PriceYearly = 990m, TrialDurationDays = 0, IsActive = true, CreatedAtUtc = SeedDate });
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(s => s.TrialStartAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.TrialEndAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.SubscriptionStartAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.SubscriptionEndAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(s => s.User).WithMany(u => u.Subscriptions).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Plan).WithMany(p => p.Subscriptions).HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId);

        builder.Ignore(s => s.IsTrialExpired);
    }
}

public class LoginAuditLogConfiguration : IEntityTypeConfiguration<LoginAuditLog>
{
    public void Configure(EntityTypeBuilder<LoginAuditLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).UseIdentityByDefaultColumn();
        builder.Property(l => l.EmailAttempted).HasMaxLength(256).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasMaxLength(1024);
        builder.Property(l => l.LoginResult).HasMaxLength(20).HasConversion<string>();
        builder.Property(l => l.FailureReason).HasMaxLength(256);
        builder.Property(l => l.AttemptedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.UserId, l.AttemptedAtUtc }).IsDescending(false, true);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SessionTokenHash).HasMaxLength(512).IsRequired();
        builder.Property(s => s.DeviceInfo).HasMaxLength(512);
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.LastActivityAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.SessionTokenHash);

        builder.Ignore(s => s.IsExpired);
        builder.Ignore(s => s.IsActive);
    }
}

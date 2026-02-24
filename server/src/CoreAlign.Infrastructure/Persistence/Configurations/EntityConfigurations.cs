using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(u => u.Username).HasMaxLength(64).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.SecurityStamp).HasMaxLength(128).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(64);
        builder.Property(u => u.LastName).HasMaxLength(64);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.TwoFactorSecretKey).HasMaxLength(256);
        builder.Property(u => u.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(u => u.UpdatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(u => u.LastLoginAtUtc).HasColumnType("datetime2(7)");

        builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("IX_Users_Username");
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("IX_Users_NormalizedEmail");

        builder.Ignore(u => u.IsLockedOut);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();
        builder.Property(r => r.Name).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(256);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(r => r.Name).IsUnique();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Property(ur => ur.AssignedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.AssignedByUser).WithMany().HasForeignKey(ur => ur.AssignedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(t => t.DeviceInfo).HasMaxLength(512);
        builder.Property(t => t.IpAddress).HasMaxLength(45);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(512);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.RevokedAtUtc).HasColumnType("datetime2(7)");

        builder.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).HasDatabaseName("IX_RefreshTokens_TokenHash");
        builder.HasIndex(t => t.UserId).HasDatabaseName("IX_RefreshTokens_UserId");

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsActive);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.UsedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).HasDatabaseName("IX_PasswordResetTokens_TokenHash");

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsValid);
    }
}

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.UsedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).HasDatabaseName("IX_EmailVerificationTokens_TokenHash");

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsValid);
    }
}

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PriceMonthly).HasColumnType("decimal(10,2)");
        builder.Property(p => p.PriceYearly).HasColumnType("decimal(10,2)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(p => p.Name).IsUnique();
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(s => s.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(s => s.TrialStartAtUtc).HasColumnType("datetime2(7)");
        builder.Property(s => s.TrialEndAtUtc).HasColumnType("datetime2(7)");
        builder.Property(s => s.SubscriptionStartAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.SubscriptionEndAtUtc).HasColumnType("datetime2(7)");
        builder.Property(s => s.CancelledAtUtc).HasColumnType("datetime2(7)");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(s => s.User).WithMany(u => u.Subscriptions).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Plan).WithMany(p => p.Subscriptions).HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(s => s.UserId).HasDatabaseName("IX_Subscriptions_UserId");

        builder.Ignore(s => s.IsTrialExpired);
    }
}

public class LoginAuditLogConfiguration : IEntityTypeConfiguration<LoginAuditLog>
{
    public void Configure(EntityTypeBuilder<LoginAuditLog> builder)
    {
        builder.ToTable("LoginAuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).UseIdentityColumn();
        builder.Property(l => l.EmailAttempted).HasMaxLength(256).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasMaxLength(1024);
        builder.Property(l => l.LoginResult).HasMaxLength(20).HasConversion<string>();
        builder.Property(l => l.FailureReason).HasMaxLength(256);
        builder.Property(l => l.AttemptedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.UserId, l.AttemptedAtUtc }).HasDatabaseName("IX_LoginAuditLogs_UserId").IsDescending(false, true);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(s => s.SessionTokenHash).HasMaxLength(512).IsRequired();
        builder.Property(s => s.DeviceInfo).HasMaxLength(512);
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.ExpiresAtUtc).HasColumnType("datetime2(7)");
        builder.Property(s => s.LastActivityAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId).HasDatabaseName("IX_UserSessions_UserId");
        builder.HasIndex(s => s.SessionTokenHash).HasDatabaseName("IX_UserSessions_TokenHash");

        builder.Ignore(s => s.IsExpired);
        builder.Ignore(s => s.IsActive);
    }
}

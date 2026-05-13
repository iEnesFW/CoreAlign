using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly CoreAlignDbContext _context;

    public UserRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}

public class TenantRepository : ITenantRepository
{
    private readonly CoreAlignDbContext _context;

    public TenantRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _context.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await _context.Tenants.AddAsync(tenant, cancellationToken);
    }
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CoreAlignDbContext _context;

    public RefreshTokenRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public void Update(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
    }
}

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly CoreAlignDbContext _context;

    public PasswordResetTokenRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
    }

    public void Update(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Update(token);
    }
}

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly CoreAlignDbContext _context;

    public EmailVerificationTokenRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
    }

    public void Update(EmailVerificationToken token)
    {
        _context.EmailVerificationTokens.Update(token);
    }
}

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly CoreAlignDbContext _context;

    public SubscriptionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == Domain.Enums.SubscriptionStatus.Active, cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
    }
}

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly CoreAlignDbContext _context;

    public SubscriptionPlanRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<List<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(cancellationToken);
    }
}

public class LoginAuditLogRepository : ILoginAuditLogRepository
{
    private readonly CoreAlignDbContext _context;

    public LoginAuditLogRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoginAuditLog log, CancellationToken cancellationToken = default)
    {
        await _context.LoginAuditLogs.AddAsync(log, cancellationToken);
    }
}

public class UserSessionRepository : IUserSessionRepository
{
    private readonly CoreAlignDbContext _context;

    public UserSessionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        await _context.UserSessions.AddAsync(session, cancellationToken);
    }

    public async Task<List<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeSessions = await GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var session in activeSessions)
        {
            session.IsRevoked = true;
        }
    }
}

public class RoleRepository : IRoleRepository
{
    private readonly CoreAlignDbContext _context;

    public RoleRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }
}

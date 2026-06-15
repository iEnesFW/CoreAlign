using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    void Update(Tenant tenant);
}

public interface ITenantSettingRepository
{
    Task<TenantSetting?> GetAsync(string category, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantSetting>> ListAsync(string? category = null, CancellationToken cancellationToken = default);
    Task UpsertAsync(string category, string key, string? value, string dataType = "string", string? description = null, bool isSensitive = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(string category, string key, CancellationToken cancellationToken = default);
}

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByCodeAsync(string code, string locale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    void Update(EmailTemplate template);
    void Remove(EmailTemplate template);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> ListChainFromAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeManyAsync(IEnumerable<Guid> refreshTokenIds, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    void Update(RefreshToken token);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    void Update(PasswordResetToken token);
}

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
    void Update(EmailVerificationToken token);
}

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
}

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

public interface ILoginAuditLogRepository
{
    Task AddAsync(LoginAuditLog log, CancellationToken cancellationToken = default);
}

public interface IUserSessionRepository
{
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<List<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
}

public interface ITwoFactorBackupCodeRepository
{
    Task AddRangeAsync(IEnumerable<TwoFactorBackupCode> codes, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TwoFactorBackupCode>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TwoFactorBackupCode?> FindActiveByHashAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default);
    void Update(TwoFactorBackupCode code);
    Task RemoveAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface ITwoFactorChallengeRepository
{
    Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default);
    Task<TwoFactorChallenge?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Update(TwoFactorChallenge challenge);
}

public interface IPasswordHistoryRepository
{
    Task<IReadOnlyList<PasswordHistory>> ListRecentByUserAsync(Guid userId, int take, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordHistory entry, CancellationToken cancellationToken = default);
    Task RemoveOlderThanAsync(Guid userId, int keep, CancellationToken cancellationToken = default);
}

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    void Update(Tag tag);
    void Remove(Tag tag);
}

public interface ICustomerTagLinkRepository
{
    Task<IReadOnlyList<CustomerTagLink>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, List<Tag>>> GetTagsByCustomersAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken = default);
    Task SyncAsync(Guid customerId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default);
    Task<bool> AttachAsync(Guid customerId, Guid tagId, CancellationToken cancellationToken = default);
    Task<bool> DetachAsync(Guid customerId, Guid tagId, CancellationToken cancellationToken = default);
    Task ReassignCustomerAsync(Guid sourceCustomerId, Guid targetCustomerId, CancellationToken cancellationToken = default);
}

using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IUserConsentRepository
{
    Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserConsent>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserConsent?> GetLatestAsync(Guid userId, string purpose, CancellationToken cancellationToken = default);
    Task<UserConsent?> GetByIdAsync(Guid consentId, CancellationToken cancellationToken = default);
    Task WithdrawAsync(Guid consentId, CancellationToken cancellationToken = default);
}

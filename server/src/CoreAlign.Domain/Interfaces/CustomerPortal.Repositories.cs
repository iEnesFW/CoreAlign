using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;

namespace CoreAlign.Domain.Interfaces;

public interface IPaymentSessionRepository
{
    Task AddAsync(PaymentSession session, CancellationToken cancellationToken = default);
    void Update(PaymentSession session);
    Task<PaymentSession?> GetByIntentAsync(string gatewayName, string intentId, CancellationToken cancellationToken = default);
    Task<PaymentSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentSession>> ListActiveByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}

public interface IUserNotificationPreferenceRepository
{
    Task<IReadOnlyList<UserNotificationPreference>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserNotificationPreference>> ListByUserTrackedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserNotificationPreference?> GetAsync(Guid userId, string notificationKind, CancellationToken cancellationToken = default);
    Task AddAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default);
    void Update(UserNotificationPreference preference);
}

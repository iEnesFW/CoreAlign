using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PaymentSessionRepository : IPaymentSessionRepository
{
    private readonly CoreAlignDbContext _context;

    public PaymentSessionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PaymentSession session, CancellationToken cancellationToken = default)
        => await _context.PaymentSessions.AddAsync(session, cancellationToken);

    public void Update(PaymentSession session) => _context.PaymentSessions.Update(session);

    public Task<PaymentSession?> GetByIntentAsync(string gatewayName, string intentId, CancellationToken cancellationToken = default)
        => _context.PaymentSessions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.GatewayName == gatewayName && s.IntentId == intentId, cancellationToken);

    public Task<PaymentSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.PaymentSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PaymentSession>> ListActiveByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentSessions
            .AsNoTracking()
            .Where(s => s.InvoiceId == invoiceId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

public class UserNotificationPreferenceRepository : IUserNotificationPreferenceRepository
{
    private readonly CoreAlignDbContext _context;

    public UserNotificationPreferenceRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserNotificationPreference>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserNotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.NotificationKind)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserNotificationPreference>> ListByUserTrackedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserNotificationPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.NotificationKind)
            .ToListAsync(cancellationToken);
    }

    public Task<UserNotificationPreference?> GetAsync(Guid userId, string notificationKind, CancellationToken cancellationToken = default)
        => _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationKind == notificationKind, cancellationToken);

    public async Task AddAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
        => await _context.UserNotificationPreferences.AddAsync(preference, cancellationToken);

    public void Update(UserNotificationPreference preference) => _context.UserNotificationPreferences.Update(preference);
}

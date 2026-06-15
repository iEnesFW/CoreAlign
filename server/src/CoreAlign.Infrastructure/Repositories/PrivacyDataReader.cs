using CoreAlign.Application.Privacy;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PrivacyDataReader : IPrivacyDataReader
{
    private readonly CoreAlignDbContext _context;

    public PrivacyDataReader(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<PersonalOrderDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.OriginCustomerUserId == userId ||
                o.OriginDealerUserId == userId ||
                o.SalesRepUserId == userId ||
                o.ApprovedByUserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new PersonalOrderDto(
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.Status.ToString(),
                o.Total))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersonalActivityDto>> GetUserActivityAsync(Guid userId, int maxRows, CancellationToken cancellationToken = default)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(maxRows)
            .Select(l => new PersonalActivityDto(
                l.CreatedAtUtc,
                l.Method,
                l.Path,
                l.StatusCode,
                l.IpAddress))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersonalMembershipDto>> GetCustomerMembershipsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CustomerUsers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _context.Customers.AsNoTracking(),
                m => m.CustomerId,
                c => c.Id,
                (m, c) => new PersonalMembershipDto(
                    m.Id,
                    c.Id,
                    c.Name,
                    m.MembershipRole.ToString(),
                    m.Status.ToString(),
                    m.InvitedAtUtc,
                    m.AcceptedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersonalMembershipDto>> GetDealerMembershipsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.DealerUsers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _context.DealerAccounts.AsNoTracking(),
                m => m.DealerAccountId,
                d => d.Id,
                (m, d) => new PersonalMembershipDto(
                    m.Id,
                    d.Id,
                    d.Name,
                    m.MembershipRole.ToString(),
                    m.Status.ToString(),
                    m.InvitedAtUtc,
                    m.AcceptedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

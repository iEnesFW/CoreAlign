using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Privacy;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PrivacyEraseService : IPrivacyEraseService
{
    private const int RetentionDays = 30;

    private readonly CoreAlignDbContext _context;

    public PrivacyEraseService(CoreAlignDbContext context) => _context = context;

    public async Task<UserEraseCascadeResult> EraseUserCascadeAsync(
        Guid userId,
        string? userEmail,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var threshold = nowUtc.AddDays(-RetentionDays);

        var customerContactsAnonymized = 0;
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            customerContactsAnonymized = await _context.CustomerContacts
                .Where(c => c.Email == userEmail)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Name, _ => "[Silinmiş Kişi]")
                    .SetProperty(c => c.Email, _ => (string?)null)
                    .SetProperty(c => c.Phone, _ => (string?)null)
                    .SetProperty(c => c.Notes, _ => (string?)null)
                    .SetProperty(c => c.Role, _ => (string?)null)
                    .SetProperty(c => c.UpdatedAtUtc, _ => nowUtc),
                    cancellationToken);
        }

        var loginAuditRows = await _context.LoginAuditLogs
            .Where(l => l.UserId == userId && l.IpAddress != null && l.AttemptedAtUtc < threshold)
            .ToListAsync(cancellationToken);
        foreach (var row in loginAuditRows)
        {
            row.IpAddressHash = HashIdentifier(row.IpAddress!);
            row.IpAddress = null;
            row.UserAgent = null;
        }

        var activityRows = await _context.ActivityLogs
            .Where(a => a.UserId == userId && (a.IpAddress != null || a.UserAgent != null) && a.CreatedAtUtc < threshold)
            .ToListAsync(cancellationToken);
        foreach (var row in activityRows)
        {
            if (row.IpAddress != null) row.IpAddressHash = HashIdentifier(row.IpAddress);
            if (row.UserAgent != null) row.UserAgentHash = HashIdentifier(row.UserAgent);
            row.IpAddress = null;
            row.UserAgent = null;
            row.UpdatedAtUtc = nowUtc;
        }

        var refreshDeleted = await _context.RefreshTokens
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var resetDeleted = await _context.PasswordResetTokens
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var verifyDeleted = await _context.EmailVerificationTokens
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var tokensDeleted = refreshDeleted + resetDeleted + verifyDeleted;

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.DeviceInfo != null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.DeviceInfo = HashIdentifier(session.DeviceInfo!);
        }

        var employees = await _context.Employees
            .Where(e => e.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var employee in employees)
        {
            employee.Anonymize(nowUtc);
        }

        var payslipsAnonymized = 0;
        if (employees.Count > 0)
        {
            var employeeIds = employees.Select(e => e.Id).ToList();
            var payslips = await _context.Payslips
                .Where(p => employeeIds.Contains(p.EmployeeId))
                .ToListAsync(cancellationToken);
            foreach (var payslip in payslips)
            {
                payslip.Anonymize(nowUtc);
            }
            payslipsAnonymized = payslips.Count;
        }

        return new UserEraseCascadeResult(
            CustomerContactsAnonymized: customerContactsAnonymized,
            LoginAuditRowsHashed: loginAuditRows.Count,
            ActivityLogRowsHashed: activityRows.Count,
            TokensDeleted: tokensDeleted,
            SessionsHashed: sessions.Count,
            EmployeesAnonymized: employees.Count,
            PayslipsAnonymized: payslipsAnonymized);
    }

    public async Task<int> AnonymizeCustomerChildrenAsync(
        Guid customerId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var addressUpdates = await _context.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Line1, _ => "[Silinmiş Adres]")
                .SetProperty(a => a.Line2, _ => (string?)null)
                .SetProperty(a => a.City, _ => (string?)null)
                .SetProperty(a => a.State, _ => (string?)null)
                .SetProperty(a => a.PostalCode, _ => (string?)null)
                .SetProperty(a => a.Country, _ => (string?)null)
                .SetProperty(a => a.UpdatedAtUtc, _ => nowUtc),
                cancellationToken);

        var contactUpdates = await _context.CustomerContacts
            .Where(c => c.CustomerId == customerId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Name, _ => "[Silinmiş Kişi]")
                .SetProperty(c => c.Role, _ => (string?)null)
                .SetProperty(c => c.Email, _ => (string?)null)
                .SetProperty(c => c.Phone, _ => (string?)null)
                .SetProperty(c => c.Notes, _ => (string?)null)
                .SetProperty(c => c.UpdatedAtUtc, _ => nowUtc),
                cancellationToken);

        return addressUpdates + contactUpdates;
    }

    public static string HashIdentifier(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}

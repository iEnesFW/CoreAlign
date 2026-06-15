using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Privacy;

public class ExportMyDataHandler : IRequestHandler<ExportMyDataQuery, PersonalDataExportDto>
{
    private const int RecentActivityRows = 250;

    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly IPrivacyDataReader _reader;
    private readonly IDataSubjectRequestLog _audit;

    public ExportMyDataHandler(
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        IPrivacyDataReader reader,
        IDataSubjectRequestLog audit)
    {
        _currentUser = currentUser;
        _users = users;
        _reader = reader;
        _audit = audit;
    }

    public async Task<PersonalDataExportDto> Handle(ExportMyDataQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new PrivacyUserNotFoundException();

        var profile = new PersonalProfileDto(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            user.UserRoles.Select(ur => ur.Role).Where(r => r is not null).Select(r => r!.Name).ToList());

        var orders = await _reader.GetUserOrdersAsync(userId, cancellationToken);
        var activity = await _reader.GetUserActivityAsync(userId, RecentActivityRows, cancellationToken);
        var customerMemberships = await _reader.GetCustomerMembershipsAsync(userId, cancellationToken);
        var dealerMemberships = await _reader.GetDealerMembershipsAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;
        await _audit.RecordExportAsync(user.TenantId, user.Id, now, cancellationToken);

        return new PersonalDataExportDto(profile, customerMemberships, dealerMemberships, orders, activity, now);
    }
}

public class EraseMyAccountHandler : IRequestHandler<EraseMyAccountCommand, ErasureResultDto>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly IUserAnonymizer _anonymizer;

    public EraseMyAccountHandler(
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        IUserAnonymizer anonymizer)
    {
        _currentUser = currentUser;
        _users = users;
        _anonymizer = anonymizer;
    }

    public async Task<ErasureResultDto> Handle(EraseMyAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new PrivacyUserNotFoundException();

        if (!string.Equals(user.Username, request.ConfirmationUsername, StringComparison.Ordinal))
        {
            throw new PrivacyConfirmationMismatchException();
        }

        var now = DateTime.UtcNow;
        await _anonymizer.AnonymizeAsync(user, now, cancellationToken);
        return new ErasureResultDto(user.Id, now, PrivacyResultMessages.AccountAnonymized);
    }
}

public class EraseCustomerByAdminHandler : IRequestHandler<EraseCustomerByAdminCommand, ErasureResultDto>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenant;
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly IUserAnonymizer _anonymizer;
    private readonly IDataSubjectRequestLog _audit;
    private readonly IPrivacyHasher _hasher;

    public EraseCustomerByAdminHandler(
        ICurrentUserAccessor currentUser,
        ITenantContext tenant,
        IUserRepository users,
        ICustomerRepository customers,
        ICustomerUserRepository customerUsers,
        IUserAnonymizer anonymizer,
        IDataSubjectRequestLog audit,
        IPrivacyHasher hasher)
    {
        _currentUser = currentUser;
        _tenant = tenant;
        _users = users;
        _customers = customers;
        _customerUsers = customerUsers;
        _anonymizer = anonymizer;
        _audit = audit;
        _hasher = hasher;
    }

    public async Task<ErasureResultDto> Handle(EraseCustomerByAdminCommand request, CancellationToken cancellationToken)
    {
        var actingUserId = _currentUser.UserIdOrThrow();
        var actingUser = await _users.GetByIdAsync(actingUserId, cancellationToken)
            ?? throw new PrivacyUserNotFoundException();

        if (!string.Equals(actingUser.Username, request.ConfirmationUsername, StringComparison.Ordinal))
        {
            throw new PrivacyConfirmationMismatchException();
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new PrivacyCustomerNotFoundException();

        if (customer.TenantId != actingUser.TenantId)
        {
            throw new PrivacyCustomerNotFoundException();
        }

        if (customer.IsAnonymized)
        {
            throw new KvkkEraseAlreadyProcessedException();
        }

        var now = DateTime.UtcNow;
        customer.Anonymize(PrivacyAnonymization.CustomerDisplayName(customer.Id));
        _customers.Update(customer);

        var memberships = await _customerUsers.ListByCustomerAsync(customer.Id, cancellationToken);
        var memberIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var members = await _users.ListByIdsAsync(memberIds, cancellationToken);
        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member.TenantId != actingUser.TenantId) continue;
            await _anonymizer.AnonymizeAsync(member, now, cancellationToken);
        }

        await _audit.RecordErasureAsync(
            customer.TenantId,
            actingUser.Id,
            _hasher.Hash(customer.TenantId, customer.Code ?? customer.Id.ToString()),
            _hasher.Hash(customer.TenantId, customer.Id.ToString()),
            now,
            cancellationToken);

        return new ErasureResultDto(customer.Id, now, PrivacyResultMessages.CustomerAnonymized);
    }
}

internal static class PrivacyResultMessages
{
    public const string AccountAnonymized = "Privacy.Result.AccountAnonymized";
    public const string CustomerAnonymized = "Privacy.Result.CustomerAnonymized";
}

internal static class PrivacyAnonymization
{
    public static string CustomerDisplayName(Guid customerId) => $"[anonymized-customer-{customerId:N}]";
}

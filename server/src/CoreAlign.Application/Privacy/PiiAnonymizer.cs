using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Privacy;

public class PiiAnonymizer : IPiiAnonymizer
{
    private const string SubjectTypeUser = "User";
    private const string SubjectTypeCustomer = "Customer";

    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly IUserAnonymizer _userAnonymizer;
    private readonly IPrivacyEraseService _eraseService;

    public PiiAnonymizer(
        IUserRepository users,
        ICustomerRepository customers,
        IUserAnonymizer userAnonymizer,
        IPrivacyEraseService eraseService)
    {
        _users = users;
        _customers = customers;
        _userAnonymizer = userAnonymizer;
        _eraseService = eraseService;
    }

    public async Task<PiiAnonymizationResult> AnonymizeUserAsync(
        Guid userId,
        bool keepFinancialTrail,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new PrivacyUserNotFoundException();

        var now = DateTime.UtcNow;
        await _userAnonymizer.AnonymizeAsync(user, now, cancellationToken);

        const int anonymizedFields = 8;
        return new PiiAnonymizationResult(
            userId,
            SubjectTypeUser,
            anonymizedFields,
            now,
            keepFinancialTrail);
    }

    public async Task<PiiAnonymizationResult> AnonymizeCustomerAsync(
        Guid customerId,
        bool keepFinancialTrail,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new PrivacyCustomerNotFoundException();

        var now = DateTime.UtcNow;
        var displayName = $"[anonymized-customer-{customer.Id:N}]";
        customer.Anonymize(displayName);
        _customers.Update(customer);

        await _eraseService.AnonymizeCustomerChildrenAsync(customer.Id, now, cancellationToken);

        const int anonymizedFields = 9;
        return new PiiAnonymizationResult(
            customerId,
            SubjectTypeCustomer,
            anonymizedFields,
            now,
            keepFinancialTrail);
    }
}

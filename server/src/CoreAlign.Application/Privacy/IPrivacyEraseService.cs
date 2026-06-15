namespace CoreAlign.Application.Privacy;

public interface IPrivacyEraseService
{
    Task<UserEraseCascadeResult> EraseUserCascadeAsync(
        Guid userId,
        string? userEmail,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<int> AnonymizeCustomerChildrenAsync(
        Guid customerId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}

public sealed record UserEraseCascadeResult(
    int CustomerContactsAnonymized,
    int LoginAuditRowsHashed,
    int ActivityLogRowsHashed,
    int TokensDeleted,
    int SessionsHashed);

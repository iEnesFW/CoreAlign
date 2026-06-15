namespace CoreAlign.Application.Privacy;

public interface IPiiAnonymizer
{
    Task<PiiAnonymizationResult> AnonymizeUserAsync(
        Guid userId,
        bool keepFinancialTrail,
        CancellationToken cancellationToken = default);

    Task<PiiAnonymizationResult> AnonymizeCustomerAsync(
        Guid customerId,
        bool keepFinancialTrail,
        CancellationToken cancellationToken = default);
}

public sealed record PiiAnonymizationResult(
    Guid SubjectId,
    string SubjectType,
    int FieldsAnonymized,
    DateTime AnonymizedAtUtc,
    bool FinancialTrailPreserved);

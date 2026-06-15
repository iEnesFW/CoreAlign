namespace CoreAlign.Application.Privacy;

public interface IPrivacyDataReader
{
    Task<IReadOnlyList<PersonalOrderDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalActivityDto>> GetUserActivityAsync(
        Guid userId,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalMembershipDto>> GetCustomerMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalMembershipDto>> GetDealerMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

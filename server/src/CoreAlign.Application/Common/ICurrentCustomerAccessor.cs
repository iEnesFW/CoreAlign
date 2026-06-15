namespace CoreAlign.Application.Common;

public interface ICurrentCustomerAccessor
{
    Task<Guid?> GetCustomerIdAsync(CancellationToken cancellationToken = default);

    Task<Guid> GetCustomerIdOrThrowAsync(CancellationToken cancellationToken = default);
}

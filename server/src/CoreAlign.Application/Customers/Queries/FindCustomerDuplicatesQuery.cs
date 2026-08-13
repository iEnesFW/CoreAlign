using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record CustomerDuplicateMatchDto(Guid Id, string Name);

/// <summary>
/// Advisory lookup for the customer form: does another record already carry this tax number,
/// national id or e-mail? It never blocks a save — the operator sees the match and decides.
/// </summary>
public record FindCustomerDuplicatesQuery(
    string? TaxNumber,
    string? NationalId,
    string? Email,
    Guid? ExcludeId) : IRequest<IReadOnlyList<CustomerDuplicateMatchDto>>;

public class FindCustomerDuplicatesQueryHandler
    : IRequestHandler<FindCustomerDuplicatesQuery, IReadOnlyList<CustomerDuplicateMatchDto>>
{
    private readonly ICustomerRepository _customers;

    public FindCustomerDuplicatesQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<IReadOnlyList<CustomerDuplicateMatchDto>> Handle(
        FindCustomerDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var matches = await _customers.FindByIdentityAsync(
            request.TaxNumber,
            request.NationalId,
            request.Email,
            request.ExcludeId,
            cancellationToken);

        return matches.Select(m => new CustomerDuplicateMatchDto(m.Id, m.Name)).ToList();
    }
}

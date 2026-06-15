using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using MediatR;

namespace CoreAlign.Application.Providers.Commands;

public record DeleteTenantProviderConfigCommand(Guid Id)
    : IRequest<Unit>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => "TenantProviderConfig";
}

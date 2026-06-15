using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Identity.PersonaPreference.Commands;

public record SetTenantUxDefaultCommand(UxComplexityMode Mode)
    : IRequest<Unit>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.Empty;
    public string AggregateType => "TenantSettings";
}

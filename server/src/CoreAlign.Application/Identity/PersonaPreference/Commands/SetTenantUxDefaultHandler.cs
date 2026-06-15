using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Identity.PersonaPreference.Commands;

public sealed class SetTenantUxDefaultHandler : IRequestHandler<SetTenantUxDefaultCommand, Unit>
{
    private readonly IPersonaPreferenceService _personaPreferenceService;
    private readonly ITenantContext _tenantContext;

    public SetTenantUxDefaultHandler(
        IPersonaPreferenceService personaPreferenceService,
        ITenantContext tenantContext)
    {
        _personaPreferenceService = personaPreferenceService;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(SetTenantUxDefaultCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        await _personaPreferenceService.SetTenantDefaultAsync(tenantId, request.Mode, cancellationToken);
        return Unit.Value;
    }
}

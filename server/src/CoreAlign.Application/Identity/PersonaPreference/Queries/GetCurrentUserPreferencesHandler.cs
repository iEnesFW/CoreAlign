using CoreAlign.Application.B2B;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Identity.PersonaPreference.Queries;

public sealed class GetCurrentUserPreferencesHandler : IRequestHandler<GetCurrentUserPreferencesQuery, UserPreferenceSnapshot>
{
    private readonly IPersonaPreferenceService _personaPreferenceService;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenantContext;

    public GetCurrentUserPreferencesHandler(
        IPersonaPreferenceService personaPreferenceService,
        ICurrentUserAccessor currentUser,
        ITenantContext tenantContext)
    {
        _personaPreferenceService = personaPreferenceService;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public Task<UserPreferenceSnapshot> Handle(GetCurrentUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var tenantId = _tenantContext.RequireTenantId();
        return _personaPreferenceService.GetSnapshotAsync(userId, tenantId, cancellationToken);
    }
}

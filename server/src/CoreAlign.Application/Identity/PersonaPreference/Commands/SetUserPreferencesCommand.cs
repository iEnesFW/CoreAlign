using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Identity.PersonaPreference.Commands;

public record SetUserPreferencesCommand(
    UxComplexityMode? Mode,
    string? LocaleOverride,
    string? ThemeOverride,
    string? PerScreenOverridesJson)
    : IRequest<UserPreferenceSnapshot>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.Empty;
    public string AggregateType => "UserPreferences";
}

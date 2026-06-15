using MediatR;

namespace CoreAlign.Application.Identity.PersonaPreference.Queries;

public record GetCurrentUserPreferencesQuery : IRequest<UserPreferenceSnapshot>;

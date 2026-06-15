using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Identity.Locale;

public sealed record SetPreferredLocaleCommand(string Locale)
    : IRequest<PreferredLocaleDto>, ITransactionalRequest;

public sealed record PreferredLocaleDto(string PreferredLocale);

using CoreAlign.Application.Providers.Commands;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Providers.Queries;

public record GetTenantProviderConfigsQuery(ProviderCategory? Category)
    : IRequest<IReadOnlyList<TenantProviderConfigDto>>;

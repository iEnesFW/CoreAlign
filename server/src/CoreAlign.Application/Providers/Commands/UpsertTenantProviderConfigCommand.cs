using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Providers.Commands;

public record UpsertTenantProviderConfigCommand(
    ProviderCategory Category,
    string ProviderName,
    string? DisplayName,
    bool IsDefault,
    bool IsEnabled,
    string? PlaintextCredentialsJson,
    int EnabledCapabilities) : IRequest<TenantProviderConfigDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.Empty;
    public string AggregateType => "TenantProviderConfig";
}

public sealed record TenantProviderConfigDto(
    Guid Id,
    string Category,
    string ProviderName,
    string? DisplayName,
    bool IsDefault,
    bool IsEnabled,
    int EnabledCapabilities,
    DateTime? LastHealthCheckUtc,
    string LastHealthStatus,
    string? LastHealthMessage);

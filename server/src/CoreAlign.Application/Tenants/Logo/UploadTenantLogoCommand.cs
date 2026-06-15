using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Tenants.Logo;

public sealed record TenantLogoDto(string LogoUrl, string StorageKey, string ContentType, long SizeBytes);

public sealed record UploadTenantLogoCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IRequest<TenantLogoDto>, ITransactionalRequest;

using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Queries;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException();

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new UserProfileDto
        {
            Id = user.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            TenantSlug = tenant.Slug,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Roles = roles,
            PreferredLocale = user.PreferredLocale
        };
    }
}

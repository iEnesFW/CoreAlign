using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException();

        user.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new AuthResponseDto
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = DateTime.MinValue,
            User = new UserProfileDto
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
            }
        };
    }
}

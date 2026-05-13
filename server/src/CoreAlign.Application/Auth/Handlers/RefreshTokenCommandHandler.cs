using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITenantRepository tenantRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tenantRepository = tenantRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
            throw new TokenExpiredException();

        var user = existingToken.User;
        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashToken(newRawRefreshToken);

        existingToken.Revoke(newRefreshTokenHash);
        _refreshTokenRepository.Update(existingToken);

        var newRefreshToken = new RefreshToken(
            user.Id,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            request.DeviceInfo,
            request.IpAddress);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email, roles);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
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
                Roles = roles
            }
        };
    }
}

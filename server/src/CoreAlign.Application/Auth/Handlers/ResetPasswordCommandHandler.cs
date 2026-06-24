using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher passwordHasher,
        IPasswordPolicyService passwordPolicyService,
        IJwtTokenService jwtTokenService,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userSessionRepository = userSessionRepository;
        _passwordHasher = passwordHasher;
        _passwordPolicyService = passwordPolicyService;
        _jwtTokenService = jwtTokenService;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.Token);
        var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsValid)
            throw new TokenExpiredException();

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException();

        await _passwordPolicyService.ValidateAsync(
            user.Id, request.NewPassword, PasswordPolicyContextFactory.For(user), cancellationToken);

        var previousHash = user.PasswordHash;
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.ResetSecurityStamp();
        _userRepository.Update(user);

        if (!string.IsNullOrEmpty(previousHash))
        {
            await _passwordPolicyService.RecordHistoryAsync(user.Id, previousHash, cancellationToken);
        }

        resetToken.MarkAsUsed();
        _passwordResetTokenRepository.Update(resetToken);

        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);
        await _userSessionRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

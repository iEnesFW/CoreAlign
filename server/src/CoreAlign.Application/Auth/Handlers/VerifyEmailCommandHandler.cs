using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, ApiResponse<bool>>
{
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailCommandHandler(
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.Token);
        var verificationToken = await _emailVerificationTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (verificationToken is null || !verificationToken.IsValid)
            throw new TokenExpiredException();

        var user = await _userRepository.GetByIdAsync(verificationToken.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException();

        user.IsEmailConfirmed = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);

        verificationToken.MarkAsUsed();
        _emailVerificationTokenRepository.Update(verificationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true);
    }
}

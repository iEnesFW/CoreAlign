using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ICaptchaVerifier captchaVerifier,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _captchaVerifier = captchaVerifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, "forgot_password", cancellationToken))
        {
            throw new CaptchaValidationException();
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is not null)
        {
            var rawToken = _jwtTokenService.GenerateRefreshToken();
            var tokenHash = _jwtTokenService.HashToken(rawToken);

            var resetToken = new PasswordResetToken(user.Id, tokenHash, DateTime.UtcNow.AddHours(1));
            await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendPasswordResetEmailAsync(user.Email, rawToken, cancellationToken);
        }

        return true;
    }
}

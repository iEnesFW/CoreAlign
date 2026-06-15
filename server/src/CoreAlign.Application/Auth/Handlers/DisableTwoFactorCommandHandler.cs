using CoreAlign.Application.Auth.Commands;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DisableTwoFactorCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITwoFactorBackupCodeRepository backupCodeRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _backupCodeRepository = backupCodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsTwoFactorEnabled && string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            throw new TwoFactorNotEnabledException();
        }

        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        user.ResetSecurityStamp();
        _userRepository.Update(user);

        await _backupCodeRepository.RemoveAllByUserAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

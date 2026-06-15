using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class RegenerateBackupCodesCommandHandler : IRequestHandler<RegenerateBackupCodesCommand, TwoFactorBackupCodesDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateBackupCodesCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITwoFactorService twoFactorService,
        ITwoFactorBackupCodeRepository backupCodeRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _twoFactorService = twoFactorService;
        _backupCodeRepository = backupCodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TwoFactorBackupCodesDto> Handle(RegenerateBackupCodesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsTwoFactorEnabled)
        {
            throw new TwoFactorNotEnabledException();
        }

        await _backupCodeRepository.RemoveAllByUserAsync(user.Id, cancellationToken);

        var plaintextCodes = _twoFactorService.GenerateBackupCodes();
        var entities = plaintextCodes
            .Select(c => new TwoFactorBackupCode(user.TenantId, user.Id, _twoFactorService.HashBackupCode(c)))
            .ToList();

        await _backupCodeRepository.AddRangeAsync(entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TwoFactorBackupCodesDto { BackupCodes = plaintextCodes.ToList() };
    }
}

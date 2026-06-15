using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class VerifyTwoFactorEnrollmentCommandHandler : IRequestHandler<VerifyTwoFactorEnrollmentCommand, TwoFactorBackupCodesDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyTwoFactorEnrollmentCommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        ITwoFactorBackupCodeRepository backupCodeRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _backupCodeRepository = backupCodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TwoFactorBackupCodesDto> Handle(VerifyTwoFactorEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecretKey))
        {
            throw new TwoFactorNotEnabledException();
        }

        if (user.IsTwoFactorEnabled)
        {
            throw new TwoFactorAlreadyEnabledException();
        }

        if (!_twoFactorService.Verify(user.TwoFactorSecretKey, request.Code))
        {
            throw new InvalidTwoFactorCodeException();
        }

        user.IsTwoFactorEnabled = true;
        user.ResetSecurityStamp();
        _userRepository.Update(user);

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

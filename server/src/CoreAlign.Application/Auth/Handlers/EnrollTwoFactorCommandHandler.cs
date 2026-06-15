using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class EnrollTwoFactorCommandHandler : IRequestHandler<EnrollTwoFactorCommand, TwoFactorEnrollmentDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITwoFactorService twoFactorService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _twoFactorService = twoFactorService;
        _unitOfWork = unitOfWork;
    }

    public async Task<TwoFactorEnrollmentDto> Handle(EnrollTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (user.IsTwoFactorEnabled)
        {
            throw new TwoFactorAlreadyEnabledException();
        }

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        var secret = _twoFactorService.GenerateSecret();
        user.TwoFactorSecretKey = secret;
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var issuer = string.IsNullOrWhiteSpace(tenant.Name) ? "CoreAlign" : tenant.Name;
        var qrUri = _twoFactorService.BuildOtpAuthUri(secret, user.Email, issuer);

        return new TwoFactorEnrollmentDto
        {
            QrCodeUri = qrUri,
            ManualKey = secret,
        };
    }
}

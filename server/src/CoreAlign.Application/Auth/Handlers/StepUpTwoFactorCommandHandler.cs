using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class StepUpTwoFactorCommandHandler : IRequestHandler<StepUpTwoFactorCommand, StepUpResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserMembershipService _userMembershipService;

    public StepUpTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IJwtTokenService jwtTokenService,
        IUserMembershipService userMembershipService)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _jwtTokenService = jwtTokenService;
        _userMembershipService = userMembershipService;
    }

    public async Task<StepUpResponseDto> Handle(StepUpTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (!user.IsTwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecretKey))
        {
            throw new TwoFactorNotEnabledException();
        }

        if (!_twoFactorService.Verify(user.TwoFactorSecretKey, request.Code))
        {
            throw new InvalidTwoFactorCodeException();
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var persona = await _userMembershipService.ResolvePersonaAsync(user.Id, user.TenantId, cancellationToken);
        var personaString = PersonaToString(persona);
        var mfaVerifiedAt = DateTime.UtcNow;
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.TenantId,
            user.Email,
            roles,
            personaString,
            mfaVerifiedAt);

        return new StepUpResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            MfaVerifiedAtUtc = mfaVerifiedAt,
        };
    }

    private static string PersonaToString(UserPersona persona) => persona switch
    {
        UserPersona.Dealer => "dealer",
        UserPersona.Customer => "customer",
        _ => "tenant",
    };
}

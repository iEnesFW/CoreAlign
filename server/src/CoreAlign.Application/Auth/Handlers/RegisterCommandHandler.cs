using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ISubscriptionRepository subscriptionRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _subscriptionRepository = subscriptionRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new DuplicateEmailException();

        if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
            throw new DuplicateUsernameException();

        var hashedPassword = _passwordHasher.Hash(request.Password);

        var user = new User(request.Username, request.Email, hashedPassword)
        {
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _userRepository.AddAsync(user, cancellationToken);

        var userRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
        if (userRole is not null)
        {
            user.UserRoles.Add(new UserRole(user.Id, userRole.Id));
        }

        var freeTrialPlan = await _subscriptionPlanRepository.GetByNameAsync("FreeTrial", cancellationToken);
        if (freeTrialPlan is not null)
        {
            var subscription = Subscription.CreateFreeTrial(user.Id, freeTrialPlan.Id, freeTrialPlan.TrialDurationDays);
            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        }

        var rawVerificationToken = _jwtTokenService.GenerateRefreshToken();
        var verificationTokenHash = _jwtTokenService.HashToken(rawVerificationToken);

        var emailVerificationToken = new EmailVerificationToken(
            user.Id,
            verificationTokenHash,
            DateTime.UtcNow.AddHours(24)
        );

        await _emailVerificationTokenRepository.AddAsync(emailVerificationToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailVerificationAsync(user.Email, rawVerificationToken, cancellationToken);

        return ApiResponse<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = DateTime.MinValue,
            User = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRole is not null ? new List<string> { userRole.Name } : new List<string>()
            }
        }, 201);
    }
}

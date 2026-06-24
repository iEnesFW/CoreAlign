using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CoreAlign.Application.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly bool _autoConfirmEmail;

    public RegisterCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ISubscriptionRepository subscriptionRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPasswordHasher passwordHasher,
        IPasswordPolicyService passwordPolicyService,
        ICaptchaVerifier captchaVerifier,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _subscriptionRepository = subscriptionRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _passwordHasher = passwordHasher;
        _passwordPolicyService = passwordPolicyService;
        _captchaVerifier = captchaVerifier;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _autoConfirmEmail = configuration.GetValue<bool>("Auth:AutoConfirmEmail");
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!await _captchaVerifier.VerifyAsync(request.CaptchaToken, "register", cancellationToken))
        {
            throw new CaptchaValidationException();
        }

        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            await _emailService.SendDuplicateRegistrationNoticeAsync(request.Email, cancellationToken);
            return BuildPendingProfile(request);
        }

        if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
            throw new DuplicateUsernameException();

        await _passwordPolicyService.ValidateAsync(
            Guid.Empty, request.Password, PasswordPolicyContext.TenantAdmin, cancellationToken);

        var tenantSlug = await GenerateUniqueSlugAsync(request.OrganizationName, cancellationToken);
        var tenant = new Tenant(request.OrganizationName, tenantSlug);
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var user = new User(tenant.Id, request.Username, request.Email, hashedPassword)
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsEmailConfirmed = _autoConfirmEmail
        };

        await _userRepository.AddAsync(user, cancellationToken);

        var adminRole = await _roleRepository.GetByNameAsync("TenantAdmin", cancellationToken);
        if (adminRole is not null)
        {
            user.UserRoles.Add(new UserRole(user.Id, adminRole.Id));
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
            DateTime.UtcNow.AddHours(24));

        await _emailVerificationTokenRepository.AddAsync(emailVerificationToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailVerificationAsync(user.Email, rawVerificationToken, cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = DateTime.MinValue,
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
                Roles = adminRole is not null ? new List<string> { adminRole.Name } : new List<string>()
            }
        };
    }

    private static AuthResponseDto BuildPendingProfile(RegisterCommand request) => new()
    {
        AccessToken = string.Empty,
        RefreshToken = string.Empty,
        ExpiresAt = DateTime.MinValue,
        User = new UserProfileDto
        {
            Id = Guid.Empty,
            TenantId = Guid.Empty,
            TenantName = request.OrganizationName,
            TenantSlug = string.Empty,
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Roles = new List<string>()
        }
    };

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = Tenant.GenerateSlug(name);
        var slug = baseSlug;
        var attempt = 0;

        while (await _tenantRepository.SlugExistsAsync(slug, cancellationToken))
        {
            attempt++;
            slug = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";
            if (attempt > 5)
            {
                throw new InvalidOperationException("Unable to generate unique tenant slug.");
            }
        }

        return slug;
    }
}

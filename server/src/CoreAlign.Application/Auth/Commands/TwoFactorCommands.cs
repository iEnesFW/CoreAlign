using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Auth.Commands;

public record EnrollTwoFactorCommand(Guid UserId)
    : IRequest<TwoFactorEnrollmentDto>, ITransactionalRequest;

public record VerifyTwoFactorEnrollmentCommand(Guid UserId, string Code)
    : IRequest<TwoFactorBackupCodesDto>, ITransactionalRequest;

public record DisableTwoFactorCommand(Guid UserId, string Password)
    : IRequest<bool>, ITransactionalRequest;

public record RegenerateBackupCodesCommand(Guid UserId, string Password)
    : IRequest<TwoFactorBackupCodesDto>, ITransactionalRequest;

public record CompleteTwoFactorChallengeCommand(
    string ChallengeToken,
    string? Code,
    string? BackupCode,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<AuthResponseDto>, ITransactionalRequest;

public record StepUpTwoFactorCommand(Guid UserId, string Code)
    : IRequest<StepUpResponseDto>, ITransactionalRequest;

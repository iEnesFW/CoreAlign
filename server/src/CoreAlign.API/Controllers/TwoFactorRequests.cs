namespace CoreAlign.API.Controllers;

public record TwoFactorVerifyRequest(string Code);

public record TwoFactorPasswordRequest(string Password);

public record TwoFactorChallengeRequest(string ChallengeToken, string? Code, string? BackupCode);

public record TwoFactorStepUpRequest(string Code);

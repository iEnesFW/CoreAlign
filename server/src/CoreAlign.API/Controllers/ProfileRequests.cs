namespace CoreAlign.API.Controllers;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateProfileRequest(string? FirstName, string? LastName, string? PhoneNumber, string? AvatarUrl);

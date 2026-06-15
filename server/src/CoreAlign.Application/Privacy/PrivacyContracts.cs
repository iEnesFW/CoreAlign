using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Privacy;

public record PersonalDataExportDto(
    PersonalProfileDto Profile,
    IReadOnlyList<PersonalMembershipDto> CustomerMemberships,
    IReadOnlyList<PersonalMembershipDto> DealerMemberships,
    IReadOnlyList<PersonalOrderDto> Orders,
    IReadOnlyList<PersonalActivityDto> RecentActivity,
    DateTime ExportedAtUtc);

public record PersonalMembershipDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string Role,
    string Status,
    DateTime JoinedAtUtc,
    DateTime? AcceptedAtUtc);

public record PersonalProfileDto(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyList<string> Roles);

public record PersonalOrderDto(
    Guid Id,
    string OrderNumber,
    DateTime OrderDate,
    string Status,
    decimal Total);

public record PersonalActivityDto(
    DateTime AtUtc,
    string Method,
    string Path,
    int StatusCode,
    string? IpAddress);

public record ErasureResultDto(
    Guid UserId,
    DateTime AnonymizedAtUtc,
    string Notice);

public record ExportMyDataQuery : IRequest<PersonalDataExportDto>, ITransactionalRequest;

public record EraseMyAccountCommand(string ConfirmationUsername)
    : IRequest<ErasureResultDto>, ITransactionalRequest;

public record EraseCustomerByAdminCommand(Guid CustomerId, string ConfirmationUsername)
    : IRequest<ErasureResultDto>, ITransactionalRequest;

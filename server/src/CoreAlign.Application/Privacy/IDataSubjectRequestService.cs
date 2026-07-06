using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Privacy;

public interface IDataSubjectRequestService
{
    Task<DataSubjectRequestDto> SubmitAsync(
        SubmitDataSubjectRequestInput input,
        CancellationToken cancellationToken = default);

    Task<DataSubjectRequestDto> ProcessAccessRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<DataSubjectRequestDto> ProcessErasureRequestAsync(
        Guid requestId,
        bool keepFinancialTrail,
        CancellationToken cancellationToken = default);

    Task<DataSubjectRequestDto> ProcessPortabilityRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<DataSubjectRequestDto> ProcessRectificationRequestAsync(
        Guid requestId,
        RectificationCorrections corrections,
        CancellationToken cancellationToken = default);

    Task<DataSubjectRequestDto> RejectAsync(
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<PagedRequestList> ListAsync(
        DataSubjectRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<DataSubjectRequestDto> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<PersonalDataExportDto> BuildExportAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}

public sealed record SubmitDataSubjectRequestInput(
    DataSubjectRequestType Type,
    Guid? RequesterUserId,
    Guid? RequesterCustomerId,
    string? RequesterEmail,
    string? Notes);

public sealed record DataSubjectRequestDto(
    Guid Id,
    Guid TenantId,
    DataSubjectRequestType Type,
    DataSubjectRequestStatus Status,
    Guid? RequesterUserId,
    Guid? RequesterCustomerId,
    DateTime SubmittedAtUtc,
    DateTime? CompletedAtUtc,
    string? RejectionReason,
    Guid? DataExportFileId,
    LegalBasisOverride LegalBasisOverride,
    string? Notes);

public sealed record RectificationCorrections(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email);

public sealed record PagedRequestList(
    IReadOnlyList<DataSubjectRequestDto> Items,
    int Total,
    int Page,
    int PageSize);

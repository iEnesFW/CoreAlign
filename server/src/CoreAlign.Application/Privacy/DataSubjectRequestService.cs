using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Privacy;

public class DataSubjectRequestService : IDataSubjectRequestService
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    private readonly IDataSubjectRequestRepository _repository;
    private readonly IUserRepository _users;
    private readonly IPiiAnonymizer _anonymizer;
    private readonly IPrivacyDataReader _reader;
    private readonly IPrivacyHasher _hasher;
    private readonly ITenantContext _tenant;

    public DataSubjectRequestService(
        IDataSubjectRequestRepository repository,
        IUserRepository users,
        IPiiAnonymizer anonymizer,
        IPrivacyDataReader reader,
        IPrivacyHasher hasher,
        ITenantContext tenant)
    {
        _repository = repository;
        _users = users;
        _anonymizer = anonymizer;
        _reader = reader;
        _hasher = hasher;
        _tenant = tenant;
    }

    public async Task<DataSubjectRequestDto> SubmitAsync(
        SubmitDataSubjectRequestInput input,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var now = DateTime.UtcNow;

        var usernameHash = !string.IsNullOrWhiteSpace(input.RequesterEmail)
            ? _hasher.Hash(tenantId, input.RequesterEmail)
            : null;

        var entity = DataSubjectRequest.Submit(
            tenantId,
            input.Type,
            now,
            input.RequesterUserId,
            input.RequesterCustomerId,
            usernameHash,
            usernameHash,
            input.Notes);

        await _repository.AddAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    public async Task<DataSubjectRequestDto> ProcessAccessRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        EnsureCanProcess(request, DataSubjectRequestType.Access);

        request.MarkInProgress(DateTime.UtcNow);

        if (request.RequesterUserId.HasValue)
        {
            _ = await _reader.GetUserOrdersAsync(request.RequesterUserId.Value, cancellationToken);
        }

        request.MarkCompleted(DateTime.UtcNow);
        _repository.Update(request);
        return ToDto(request);
    }

    public async Task<DataSubjectRequestDto> ProcessErasureRequestAsync(
        Guid requestId,
        bool keepFinancialTrail,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        EnsureCanProcess(request, DataSubjectRequestType.Erasure);

        request.MarkInProgress(DateTime.UtcNow);

        if (request.RequesterUserId.HasValue)
        {
            await _anonymizer.AnonymizeUserAsync(request.RequesterUserId.Value, keepFinancialTrail, cancellationToken);
        }
        else if (request.RequesterCustomerId.HasValue)
        {
            await _anonymizer.AnonymizeCustomerAsync(request.RequesterCustomerId.Value, keepFinancialTrail, cancellationToken);
        }

        request.MarkCompleted(DateTime.UtcNow);
        _repository.Update(request);
        return ToDto(request);
    }

    public async Task<DataSubjectRequestDto> ProcessPortabilityRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        EnsureCanProcess(request, DataSubjectRequestType.Portability);

        request.MarkInProgress(DateTime.UtcNow);
        request.MarkCompleted(DateTime.UtcNow);
        _repository.Update(request);
        return ToDto(request);
    }

    public async Task<DataSubjectRequestDto> ProcessRectificationRequestAsync(
        Guid requestId,
        RectificationCorrections corrections,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        EnsureCanProcess(request, DataSubjectRequestType.Rectification);

        request.MarkInProgress(DateTime.UtcNow);

        if (request.RequesterUserId.HasValue)
        {
            var user = await _users.GetByIdAsync(request.RequesterUserId.Value, cancellationToken)
                ?? throw new PrivacyUserNotFoundException();

            ApplyCorrections(user, corrections);
            _users.Update(user);
        }

        request.MarkCompleted(DateTime.UtcNow);
        _repository.Update(request);
        return ToDto(request);
    }

    public async Task<DataSubjectRequestDto> RejectAsync(
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        if (request.Status is DataSubjectRequestStatus.Completed or DataSubjectRequestStatus.Rejected)
        {
            throw new DataSubjectRequestInvalidStateException("Privacy.RequestAlreadyClosed");
        }

        request.MarkRejected(DateTime.UtcNow, reason);
        _repository.Update(request);
        return ToDto(request);
    }

    public async Task<PagedRequestList> ListAsync(
        DataSubjectRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePage = page <= 0 ? 1 : page;
        var effectiveSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var skip = (effectivePage - 1) * effectiveSize;
        var items = await _repository.ListAsync(status, skip, effectiveSize, cancellationToken);
        var total = await _repository.CountAsync(status, cancellationToken);

        return new PagedRequestList(
            items.Select(ToDto).ToList(),
            total,
            effectivePage,
            effectiveSize);
    }

    public async Task<DataSubjectRequestDto> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        return ToDto(request);
    }

    private async Task<DataSubjectRequest> LoadAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new DataSubjectRequestNotFoundException();
    }

    private static void EnsureCanProcess(DataSubjectRequest request, DataSubjectRequestType expectedType)
    {
        if (request.RequestType != expectedType)
        {
            throw new DataSubjectRequestInvalidStateException("Privacy.RequestTypeMismatch");
        }

        if (request.Status is DataSubjectRequestStatus.Completed or DataSubjectRequestStatus.Rejected)
        {
            throw new DataSubjectRequestInvalidStateException("Privacy.RequestAlreadyClosed");
        }
    }

    private static void ApplyCorrections(User user, RectificationCorrections corrections)
    {
        if (corrections.FirstName is not null) user.FirstName = corrections.FirstName;
        if (corrections.LastName is not null) user.LastName = corrections.LastName;
        if (corrections.PhoneNumber is not null) user.PhoneNumber = corrections.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(corrections.Email))
        {
            user.Email = corrections.Email;
            user.NormalizedEmail = corrections.Email.ToUpperInvariant();
        }
        user.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static DataSubjectRequestDto ToDto(DataSubjectRequest entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.RequestType,
            entity.Status,
            entity.RequesterUserId,
            entity.RequesterCustomerId,
            entity.SubmittedAtUtc,
            entity.CompletedAtUtc,
            entity.RejectionReason,
            entity.DataExportFileId,
            entity.LegalBasisOverride,
            entity.Notes);
}

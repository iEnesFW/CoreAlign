using CoreAlign.Application.B2B;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Application.Returns.Mapping;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Returns.Handlers;

public class ApproveReturnRequestCommandHandler : IRequestHandler<ApproveReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveReturnRequestCommandHandler(
        IReturnRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnRequestDto> Handle(ApproveReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new ReturnRequestNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);

        var approverId = _currentUser.UserIdOrThrow();
        entity.Approve(approverId);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReturnRequestMapper.ToDto(entity);
    }
}

public class RejectReturnRequestCommandHandler : IRequestHandler<RejectReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RejectReturnRequestCommandHandler(
        IReturnRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnRequestDto> Handle(RejectReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new ReturnRequestNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);

        var rejectorId = _currentUser.UserIdOrThrow();
        entity.Reject(rejectorId, request.Reason);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReturnRequestMapper.ToDto(entity);
    }
}

public class CancelReturnRequestCommandHandler : IRequestHandler<CancelReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CancelReturnRequestCommandHandler(
        IReturnRequestRepository repository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnRequestDto> Handle(CancelReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new ReturnRequestNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);

        entity.Cancel();
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReturnRequestMapper.ToDto(entity);
    }
}

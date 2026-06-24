using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Observability;

public sealed record ResolveErrorLogCommand(Guid Id, string? Notes) : IRequest<Unit>;

public sealed class ResolveErrorLogHandler : IRequestHandler<ResolveErrorLogCommand, Unit>
{
    private readonly IErrorLogRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ResolveErrorLogHandler(
        IErrorLogRepository repository,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ResolveErrorLogCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ErrorLogNotFoundException(request.Id);

        entry.MarkResolved(_currentUser.UserId, request.Notes, DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

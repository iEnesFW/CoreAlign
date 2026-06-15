using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Common.Behaviors;

public sealed class SaveChangesBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public SaveChangesBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        if (request is ITransactionalRequest)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return response;
    }
}

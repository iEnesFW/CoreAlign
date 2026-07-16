
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Application.Manufacturing.Mapping;
using CoreAlign.Application.Manufacturing.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

public class ProductionJobQueryHandlers :
    IRequestHandler<ListProductionJobsQuery, IReadOnlyList<ProductionJobListDto>>,
    IRequestHandler<GetProductionJobByIdQuery, ProductionJobDetailDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly IProductionJobRepository _jobRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWorkCenterRepository _workCenterRepository;

    public ProductionJobQueryHandlers(
        ITenantContext tenantContext,
        IProductionJobRepository jobRepository,
        IProductRepository productRepository,
        IWorkCenterRepository workCenterRepository)
    {
        _tenantContext = tenantContext;
        _jobRepository = jobRepository;
        _productRepository = productRepository;
        _workCenterRepository = workCenterRepository;
    }

    public async Task<IReadOnlyList<ProductionJobListDto>> Handle(ListProductionJobsQuery request, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        var rows = await _jobRepository.ListAsync(
            tenantId,
            request.Status,
            request.ProductId,
            request.Take,
            ct);

        return rows.Select(ProductionJobMapper.ToListDto).ToList();
    }

    public async Task<ProductionJobDetailDto> Handle(GetProductionJobByIdQuery request, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        var job = await _jobRepository.GetByIdAsync(tenantId, request.Id, ct)
            ?? throw new ProductionJobNotFoundException(request.Id);

        var product = await _productRepository.GetByIdAsync(job.ProductId, ct);
        var productName = product?.Name ?? "Unknown";

        var workCenterIds = job.Steps.Select(s => s.WorkCenterId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var workCenters = await _workCenterRepository.GetByIdsAsync(workCenterIds, ct);
        var workCenterNames = workCenters.ToDictionary(w => w.Id, w => w.Name);

        return ProductionJobMapper.ToDetailDto(job, productName, workCenterNames);
    }
}


using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Application.Manufacturing.Mapping;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

public class ProductionJobCommandHandlers :
    IRequestHandler<CreateProductionJobCommand, ProductionJobDetailDto>,
    IRequestHandler<ReleaseProductionJobCommand, ProductionJobDetailDto>,
    IRequestHandler<StartJobStepCommand, ProductionJobDetailDto>,
    IRequestHandler<FinishJobStepCommand, ProductionJobDetailDto>,
    IRequestHandler<SkipJobStepCommand, ProductionJobDetailDto>,
    IRequestHandler<ReworkToStepCommand, ProductionJobDetailDto>,
    IRequestHandler<PutJobOnHoldCommand, ProductionJobDetailDto>,
    IRequestHandler<ResumeJobCommand, ProductionJobDetailDto>,
    IRequestHandler<CancelProductionJobCommand, ProductionJobDetailDto>,
    IRequestHandler<CompleteProductionJobCommand, ProductionJobDetailDto>,
    IRequestHandler<ConvertPlannedOrderToJobCommand, ProductionJobDetailDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly IProductionJobRepository _jobRepository;
    private readonly IProductionRoutingRepository _routingRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWorkCenterRepository _workCenterRepository;
    private readonly IPlannedProductionOrderRepository _plannedOrderRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IDateTimeProvider _dateTime;

    public ProductionJobCommandHandlers(
        ITenantContext tenantContext,
        IProductionJobRepository jobRepository,
        IProductionRoutingRepository routingRepository,
        IProductRepository productRepository,
        IWorkCenterRepository workCenterRepository,
        IDocumentSequenceRepository sequenceRepository,
        IPlannedProductionOrderRepository plannedOrderRepository,
        IStockMovementRepository stockMovementRepository,
        IDateTimeProvider dateTime)
    {
        _tenantContext = tenantContext;
        _jobRepository = jobRepository;
        _routingRepository = routingRepository;
        _productRepository = productRepository;
        _workCenterRepository = workCenterRepository;
        _sequenceRepository = sequenceRepository;
        _plannedOrderRepository = plannedOrderRepository;
        _stockMovementRepository = stockMovementRepository;
        _dateTime = dateTime;
    }

    public async Task<ProductionJobDetailDto> Handle(CreateProductionJobCommand request, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        var now = _dateTime.UtcNow;

        var product = await _productRepository.GetByIdAsync(request.ProductId, ct)
            ?? throw new ProductNotFoundException();

        var jobNumber = await _sequenceRepository.ConsumeAsync(DocumentSequenceType.ProductionJobNumber, now, ct);

        var job = new ProductionJob(
            jobNumber,
            request.ProductId,
            request.PlannedQuantity,
            request.UnitOfMeasure,
            null, // SourcePlannedProductionOrderId (Phase 3B)
            request.WarehouseId,
            request.PlannedStartDateUtc,
            request.DueDateUtc,
            request.Notes);

        if (request.RoutingId.HasValue)
        {
            var routing = await _routingRepository.GetByIdReadAsync(tenantId, request.RoutingId.Value, ct)
                ?? throw new RoutingNotFoundException(request.RoutingId.Value);

            if (routing.Status != RoutingStatus.Active)
            {
                throw new RoutingNotActiveForJobException(routing.Id);
            }

            var snapshots = routing.Steps.Select(s => new ProductionJobStepSnapshot(
                s.StepNumber,
                s.WorkCenterId,
                s.Id,
                s.OperationName,
                s.OperationType,
                s.SetupTimeMinutes,
                s.RunTimeMinutesPerUnit,
                s.RunTimeMinutesPerSqm,
                s.ScrapPercentage,
                s.Instructions,
                s.IsOptional)).ToList();

            job.SnapshotRouting(
                routing.Id,
                routing.Code,
                routing.Name,
                routing.ConcurrencyToken, // using version
                snapshots);
        }

        await _jobRepository.AddAsync(job, ct);
        
        return await BuildDetailDtoAsync(job, product.Name, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(ReleaseProductionJobCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.Id, ct);
        job.Release(request.WarehouseId, _dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(StartJobStepCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.JobId, ct);
        job.StartStep(request.StepNumber, request.OperatorId, _dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(FinishJobStepCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.JobId, ct);
        job.FinishStep(
            request.StepNumber,
            request.GoodQuantity,
            request.ScrappedQuantity,
            request.ScrapReasonCodeId,
            request.ActualSetupMinutes,
            request.ActualRunMinutes,
            request.OperatorId,
            _dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(SkipJobStepCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.JobId, ct);
        job.SkipStep(request.StepNumber, _dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(ReworkToStepCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.JobId, ct);
        job.ReworkToStep(request.TargetStepNumber, request.FromStepNumber, request.Reason, _dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(PutJobOnHoldCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.Id, ct);
        job.PutOnHold(_dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(ResumeJobCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.Id, ct);
        job.Resume(_dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(CancelProductionJobCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.Id, ct);
        job.Cancel(request.Reason, _dateTime.UtcNow);
        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(CompleteProductionJobCommand request, CancellationToken ct)
    {
        var job = await GetJobAsync(request.Id, ct);
        job.MarkCompleted(request.CompletedQuantity, request.WarehouseId, _dateTime.UtcNow);
        
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        // Phase 3B Stock Movement Logic
        var product = await _productRepository.GetByIdAsync(job.ProductId, ct);
        var cost = product?.StandardCost ?? 0m;
        
        var movement = new StockMovement(
            job.ProductId,
            request.WarehouseId,
            StockMovementType.Receipt,
            request.CompletedQuantity,
            cost, // unitCost
            request.CompletedQuantity, // onHandAfter (simplified for now, ideally needs latest onHand calculation)
            cost, // avgCostAfter
            _dateTime.UtcNow,
            StockSourceDocumentType.Production,
            job.Id,
            null,
            job.JobNumber,
            null,
            null,
            null,
            null, // UserId
            "Completed Production Job");
            
        await _stockMovementRepository.AddAsync(movement, ct);

        return await BuildDetailDtoAsync(job, null, ct);
    }

    public async Task<ProductionJobDetailDto> Handle(ConvertPlannedOrderToJobCommand request, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        var now = _dateTime.UtcNow;

        var plannedOrder = await _plannedOrderRepository.GetByIdAsync(request.PlannedOrderId, ct)
            ?? throw new PlannedProductionOrderNotFoundException(request.PlannedOrderId);
            
        plannedOrder.Release();

        var product = await _productRepository.GetByIdAsync(plannedOrder.ProductId, ct)
            ?? throw new ProductNotFoundException();

        var jobNumber = await _sequenceRepository.ConsumeAsync(DocumentSequenceType.ProductionJobNumber, now, ct);

        var job = new ProductionJob(
            jobNumber,
            plannedOrder.ProductId,
            plannedOrder.Quantity,
            request.UnitOfMeasure,
            plannedOrder.Id, // SourcePlannedProductionOrderId
            request.WarehouseId,
            plannedOrder.ReleaseDateUtc,
            plannedOrder.DueDateUtc,
            request.Notes);

        if (request.RoutingId.HasValue)
        {
            var routing = await _routingRepository.GetByIdReadAsync(tenantId, request.RoutingId.Value, ct)
                ?? throw new RoutingNotFoundException(request.RoutingId.Value);

            if (routing.Status != RoutingStatus.Active)
            {
                throw new RoutingNotActiveForJobException(routing.Id);
            }

            var snapshots = routing.Steps.Select(s => new ProductionJobStepSnapshot(
                s.StepNumber,
                s.WorkCenterId,
                s.Id,
                s.OperationName,
                s.OperationType,
                s.SetupTimeMinutes,
                s.RunTimeMinutesPerUnit,
                s.RunTimeMinutesPerSqm,
                s.ScrapPercentage,
                s.Instructions,
                s.IsOptional)).ToList();

            job.SnapshotRouting(
                routing.Id,
                routing.Code,
                routing.Name,
                routing.ConcurrencyToken,
                snapshots);
        }

        await _jobRepository.AddAsync(job, ct);
        return await BuildDetailDtoAsync(job, product.Name, ct);
    }

    private async Task<ProductionJob> GetJobAsync(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        return await _jobRepository.GetByIdAsync(tenantId, id, ct)
            ?? throw new ProductionJobNotFoundException(id);
    }

    private async Task<ProductionJobDetailDto> BuildDetailDtoAsync(ProductionJob job, string? productNameOpt, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? throw new MissingTenantContextException();
        var productName = productNameOpt;
        if (productName == null)
        {
            var p = await _productRepository.GetByIdAsync(job.ProductId, ct);
            productName = p?.Name ?? "Unknown";
        }

        var workCenterIds = job.Steps.Select(s => s.WorkCenterId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var workCenters = await _workCenterRepository.GetByIdsAsync(workCenterIds, ct);
        var workCenterNames = workCenters.ToDictionary(w => w.Id, w => w.Name);

        return ProductionJobMapper.ToDetailDto(job, productName, workCenterNames);
    }
}

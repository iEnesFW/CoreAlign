using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class GenerateShareTokenCommandHandler : IRequestHandler<GenerateShareTokenCommand, ShareTokenInfoDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectSceneRepository _sceneRepo;
    private readonly IGlassProjectShareTokenRepository _tokenRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly IShareTokenService _generator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IStockAvailabilityService _availabilityService;

    public GenerateShareTokenCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectSceneRepository sceneRepo,
        IGlassProjectShareTokenRepository tokenRepo,
        IGlassEnclosureSettingsRepository settingsRepo,
        IShareTokenService generator,
        ICurrentUserAccessor currentUser,
        IStockAvailabilityService availabilityService)
    {
        _projectRepo = projectRepo;
        _sceneRepo = sceneRepo;
        _tokenRepo = tokenRepo;
        _settingsRepo = settingsRepo;
        _generator = generator;
        _currentUser = currentUser;
        _availabilityService = availabilityService;
    }

    public async Task<ShareTokenInfoDto> Handle(GenerateShareTokenCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var latestScene = await _sceneRepo.GetLatestAsync(project.Id, cancellationToken);
        var version = latestScene?.Version ?? project.CurrentSceneVersion;
        if (version <= 0)
        {
            throw new InvalidOperationException("Project must have a saved scene before generating a share link.");
        }

        if (project.IsBomStale && !request.ForceWithStaleBom)
        {
            throw new BomStaleBlocksShareException(project.BomStaleReason);
        }

        if (!request.ForceWithShortage)
        {
            var availability = await _availabilityService.CheckAsync(project.Id, warehouseId: null, cancellationToken);
            var shortageLineIds = availability
                .Where(a => a.HasShortage)
                .Select(a => a.BomLineId)
                .ToList();
            if (shortageLineIds.Count > 0)
            {
                throw new StockShortageBlocksConvertException(shortageLineIds);
            }
        }

        var ttlDays = request.Data.OverrideTtlDays ?? settings.QuoteShareTokenTtlDays;
        var expiresAt = DateTime.UtcNow.AddDays(Math.Max(1, ttlDays));
        var tokenValue = _generator.GenerateToken();
        var entity = new GlassProjectShareToken(project.Id, version, tokenValue, expiresAt, _currentUser.UserId ?? Guid.Empty);
        await _tokenRepo.AddAsync(entity, cancellationToken);

        if (project.Status == GlassProjectStatus.Draft || project.Status == GlassProjectStatus.Surveyed)
        {
            project.TransitionTo(GlassProjectStatus.Quoted, _currentUser.UserId ?? Guid.Empty);
            _projectRepo.Update(project);
        }
        project.RaiseQuotedDomainEvent(entity.Id, tokenValue);
        _projectRepo.Update(project);

        return Map(entity);
    }

    internal static ShareTokenInfoDto Map(GlassProjectShareToken token) => new(
        token.Id, token.Token, $"/share/glass/{token.Token}",
        token.SceneVersion, token.ExpiresAtUtc, token.ViewCount,
        token.LastViewedAtUtc, token.AcceptedAtUtc, token.RejectedAtUtc, token.RejectionReason);
}

public class GetShareTokensQueryHandler : IRequestHandler<GetShareTokensQuery, IReadOnlyList<ShareTokenInfoDto>>
{
    private readonly IGlassProjectShareTokenRepository _tokenRepo;

    public GetShareTokensQueryHandler(IGlassProjectShareTokenRepository tokenRepo) => _tokenRepo = tokenRepo;

    public async Task<IReadOnlyList<ShareTokenInfoDto>> Handle(GetShareTokensQuery request, CancellationToken cancellationToken)
    {
        var tokens = await _tokenRepo.ListByProjectAsync(request.ProjectId, cancellationToken);
        return tokens.Select(GenerateShareTokenCommandHandler.Map).ToList();
    }
}

public class GetShareViewerProjectQueryHandler : IRequestHandler<GetShareViewerProjectQuery, ShareViewerProjectDto?>
{
    private readonly IGlassProjectShareTokenRepository _tokenRepo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectSceneRepository _sceneRepo;
    private readonly ISceneCompressor _compressor;
    private readonly ICustomerRepository _customerRepo;

    public GetShareViewerProjectQueryHandler(
        IGlassProjectShareTokenRepository tokenRepo,
        IGlassProjectRepository projectRepo,
        IGlassProjectSceneRepository sceneRepo,
        ISceneCompressor compressor,
        ICustomerRepository customerRepo)
    {
        _tokenRepo = tokenRepo;
        _projectRepo = projectRepo;
        _sceneRepo = sceneRepo;
        _compressor = compressor;
        _customerRepo = customerRepo;
    }

    public async Task<ShareViewerProjectDto?> Handle(GetShareViewerProjectQuery request, CancellationToken cancellationToken)
    {
        var token = await _tokenRepo.GetByTokenAsync(request.Token, cancellationToken);
        if (token is null) return null;
        if (token.ExpiresAtUtc < DateTime.UtcNow) throw new GlassShareTokenExpiredException();

        token.RegisterView();
        _tokenRepo.Update(token);

        var scene = await _sceneRepo.GetByVersionAsync(token.ProjectId, token.SceneVersion, cancellationToken);
        if (scene is null) return null;
        var sceneJson = _compressor.Decompress(scene.SceneJsonCompressed);

        var project = await _projectRepo.GetByIdAsync(token.ProjectId, cancellationToken);
        if (project is null) return null;
        var customer = await _customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);

        return new ShareViewerProjectDto(
            project.Id,
            project.Code,
            project.ProjectName,
            customer?.Name,
            project.Status.ToString(),
            project.Currency,
            project.GrandTotal,
            token.SceneVersion,
            sceneJson,
            token.ExpiresAtUtc,
            token.AcceptedAtUtc.HasValue || token.RejectedAtUtc.HasValue);
    }
}

public class RecordShareViewerActionCommandHandler : IRequestHandler<RecordShareViewerActionCommand, ShareViewerActionResultDto>
{
    private readonly IGlassProjectShareTokenRepository _tokenRepo;
    private readonly IFileStorage _fileStorage;

    public RecordShareViewerActionCommandHandler(
        IGlassProjectShareTokenRepository tokenRepo,
        IFileStorage fileStorage)
    {
        _tokenRepo = tokenRepo;
        _fileStorage = fileStorage;
    }

    public async Task<ShareViewerActionResultDto> Handle(RecordShareViewerActionCommand request, CancellationToken cancellationToken)
    {
        var token = await _tokenRepo.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new GlassShareTokenExpiredException();
        if (token.ExpiresAtUtc < DateTime.UtcNow) throw new GlassShareTokenExpiredException();
        if (token.AcceptedAtUtc.HasValue || token.RejectedAtUtc.HasValue)
        {
            throw new GlassQuoteAlreadyAcceptedException();
        }

        if (request.Data.Accept)
        {
            var signatureUrl = await PersistSignatureAsync(token.Token, request.Data.SignatureDataUrl, cancellationToken);
            token.Accept(signatureUrl);
        }
        else
        {
            token.Reject(request.Data.Reason);
        }
        _tokenRepo.Update(token);

        return new ShareViewerActionResultDto(
            token.AcceptedAtUtc.HasValue,
            token.RejectedAtUtc.HasValue,
            token.AcceptedAtUtc ?? token.RejectedAtUtc ?? DateTime.UtcNow);
    }

    private async Task<string?> PersistSignatureAsync(string tokenLabel, string? dataUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0) return null;
        var base64 = dataUrl[(commaIndex + 1)..];
        var bytes = Convert.FromBase64String(base64);
        await using var stream = new MemoryStream(bytes);
        var stored = await _fileStorage.SaveAsync("share-signatures", $"{tokenLabel}.png", stream, "image/png", ct);
        return stored.PublicUrl;
    }
}

public class ConvertProjectToOrderCommandHandler : IRequestHandler<ConvertProjectToOrderCommand, ConvertProjectToOrderResultDto>
{
    private const decimal DefaultTaxRatePercent = 20m;

    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectOrderLinkRepository _linkRepo;
    private readonly IGlassProjectBOMLineRepository _bomLineRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IDocumentSequenceRepository _sequenceRepo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IStockAvailabilityService _availabilityService;
    private readonly IGlassEnclosureSettingsRepository? _settingsRepo;

    public ConvertProjectToOrderCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectOrderLinkRepository linkRepo,
        IGlassProjectBOMLineRepository bomLineRepo,
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IDocumentSequenceRepository sequenceRepo,
        ICurrentUserAccessor currentUser,
        IStockAvailabilityService availabilityService,
        IGlassEnclosureSettingsRepository? settingsRepo = null)
    {
        _projectRepo = projectRepo;
        _linkRepo = linkRepo;
        _bomLineRepo = bomLineRepo;
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _sequenceRepo = sequenceRepo;
        _currentUser = currentUser;
        _availabilityService = availabilityService;
        _settingsRepo = settingsRepo;
    }

    public async Task<ConvertProjectToOrderResultDto> Handle(ConvertProjectToOrderCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();

        var existing = await _linkRepo.GetByProjectAsync(project.Id, cancellationToken);
        if (existing is not null)
        {
            var existingOrder = await _orderRepo.GetByIdAsync(existing.OrderId, cancellationToken)
                ?? throw new InvalidOperationException("Linked order is missing.");
            return new ConvertProjectToOrderResultDto(project.Id, existingOrder.Id, existingOrder.OrderNumber, existing.LinkedAtUtc);
        }

        var bomLines = await _bomLineRepo.ListByProjectAsync(project.Id, cancellationToken);
        if (bomLines.Count == 0)
        {
            throw new EmptyBomException();
        }

        var unlinkedNonService = bomLines.FirstOrDefault(l => !l.IsService && l.ProductId is null);
        if (unlinkedNonService is not null)
        {
            throw new BomLineProductLinkMissingException(unlinkedNonService.Id);
        }

        if (project.IsBomStale && !request.ForceWithStaleBom)
        {
            throw new BomStaleBlocksConvertException(project.BomStaleReason);
        }

        if (!request.ForceConvertWithShortage)
        {
            var availability = await _availabilityService.CheckAsync(project.Id, warehouseId: null, cancellationToken);
            var shortageLineIds = availability
                .Where(a => a.HasShortage)
                .Select(a => a.BomLineId)
                .ToList();
            if (shortageLineIds.Count > 0)
            {
                throw new StockShortageBlocksConvertException(shortageLineIds);
            }
        }

        var substituteSelections = request.SubstituteSelections ?? new Dictionary<Guid, Guid>();
        var stockProductIds = bomLines
            .Where(l => !l.IsService && l.ProductId.HasValue)
            .Select(l => substituteSelections.TryGetValue(l.Id, out var chosen) ? chosen : l.ProductId!.Value)
            .Distinct()
            .ToList();
        var products = stockProductIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : await _productRepo.GetByIdsAsync(stockProductIds, cancellationToken);

        var marginPercent = _settingsRepo is null
            ? 0m
            : (await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken)).DefaultMarginPercent;
        var marginMultiplier = 1m + (marginPercent / 100m);
        var taxRatePercent = DefaultTaxRatePercent;

        await _sequenceRepo.EnsureExistsAsync(DocumentSequenceType.OrderNumber, prefix: "SO", padLength: 6, year: DateTime.UtcNow.Year, cancellationToken);
        var orderNumber = await _sequenceRepo.ConsumeAsync(DocumentSequenceType.OrderNumber, DateTime.UtcNow, cancellationToken);
        var order = new Order(orderNumber, project.CustomerId, DateTime.UtcNow, project.Currency, $"Glass Enclosure {project.Code}");
        order.UpdateDetails(
            type: order.Type,
            source: order.Source,
            requestedDeliveryDate: null,
            promisedDeliveryDate: null,
            billingAddressId: null,
            shippingAddressId: null,
            paymentTermsId: null,
            priceListId: null,
            exchangeRate: project.FxRateToBase > 0 ? project.FxRateToBase : 1m,
            shippingCost: 0m,
            headerDiscountPercent: 0m,
            headerDiscountAmount: 0m,
            salesRepUserId: project.AssignedSalespersonUserId,
            channel: null,
            internalNotes: null,
            customerNotes: null,
            originOrderId: null);
        order.LinkToGlassProject(project.Id);

        var orderedBomLines = bomLines.OrderBy(l => l.SortOrder).ThenBy(l => l.Description).ToList();
        var orderLines = new List<OrderLine>(orderedBomLines.Count);
        foreach (var bomLine in orderedBomLines)
        {
            string sku;
            string name;
            Guid productIdForLine;
            Guid? substituteFromProductId = null;
            if (bomLine.IsService)
            {
                sku = "SERVICE";
                name = bomLine.Description;
                productIdForLine = Guid.Empty;
            }
            else
            {
                var originalProductId = bomLine.ProductId!.Value;
                var chosenProductId = substituteSelections.TryGetValue(bomLine.Id, out var s)
                    ? s
                    : originalProductId;
                var product = products[chosenProductId];
                sku = string.IsNullOrWhiteSpace(product.Sku) ? "PROD" : product.Sku;
                name = string.IsNullOrWhiteSpace(product.Name) ? bomLine.Description : product.Name;
                productIdForLine = product.Id;
                if (chosenProductId != originalProductId)
                {
                    substituteFromProductId = originalProductId;
                }
            }

            var sellingUnitPrice = decimal.Round(bomLine.UnitCost * marginMultiplier, 4);
            var lineTaxRate = bomLine.IsService ? 0m : taxRatePercent;

            var orderLine = new OrderLine(
                productIdForLine,
                sku,
                name,
                bomLine.Quantity,
                sellingUnitPrice,
                sourceBomLineId: bomLine.Id,
                sourceProjectId: project.Id,
                isService: bomLine.IsService,
                substituteFromProductId: substituteFromProductId);
            orderLine.ApplyPricing(
                quantity: bomLine.Quantity,
                listPriceSnapshot: sellingUnitPrice,
                unitPrice: sellingUnitPrice,
                lineDiscountPercent: 0m,
                lineDiscountAmount: 0m,
                isManualPriceOverride: false,
                taxRatePercent: lineTaxRate,
                taxRateId: null,
                isTaxInclusive: false,
                withholdingRatePercent: 0m,
                unitCostSnapshot: bomLine.UnitCost,
                uomId: null,
                uomCode: bomLine.Unit,
                uomConversionFactor: 1m,
                warehouseId: null,
                lineNotes: null,
                parentLineId: null,
                isKitComponent: false,
                productDescriptionSnapshot: bomLine.Description);
            orderLines.Add(orderLine);
        }
        order.ReplaceLines(orderLines);

        await _orderRepo.AddAsync(order, cancellationToken);

        var link = new GlassProjectOrderLink(project.Id, order.Id);
        await _linkRepo.AddAsync(link, cancellationToken);

        project.TransitionTo(GlassProjectStatus.Confirmed, _currentUser.UserId ?? Guid.Empty);
        _projectRepo.Update(project);

        return new ConvertProjectToOrderResultDto(project.Id, order.Id, order.OrderNumber, link.LinkedAtUtc);
    }
}

public class ReleaseToProductionCommandHandler : IRequestHandler<ReleaseToProductionCommand, GlassWorkOrderDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectOrderLinkRepository _linkRepo;
    private readonly IGlassWorkOrderRepository _workOrderRepo;
    private readonly IGlassProjectBOMLineRepository _bomLineRepo;
    private readonly IGlassProjectCuttingPlanRepository _cuttingPlanRepo;
    private readonly IGlassWorkOrderRevisionRepository _revisionRepo;
    private readonly IProductionScheduler _scheduler;
    private readonly ICurrentUserAccessor _currentUser;

    public ReleaseToProductionCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectOrderLinkRepository linkRepo,
        IGlassWorkOrderRepository workOrderRepo,
        IGlassProjectBOMLineRepository bomLineRepo,
        IGlassProjectCuttingPlanRepository cuttingPlanRepo,
        IGlassWorkOrderRevisionRepository revisionRepo,
        IProductionScheduler scheduler,
        ICurrentUserAccessor currentUser)
    {
        _projectRepo = projectRepo;
        _linkRepo = linkRepo;
        _workOrderRepo = workOrderRepo;
        _bomLineRepo = bomLineRepo;
        _cuttingPlanRepo = cuttingPlanRepo;
        _revisionRepo = revisionRepo;
        _scheduler = scheduler;
        _currentUser = currentUser;
    }

    public async Task<GlassWorkOrderDto> Handle(ReleaseToProductionCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        if (project.Status != GlassProjectStatus.Confirmed)
        {
            throw new GlassProjectInvalidStatusTransitionException(project.Status.ToString(), GlassProjectStatus.InProduction.ToString());
        }
        var link = await _linkRepo.GetByProjectAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException("Project must be converted to an order before releasing to production.");

        var workloadM2 = project.TotalAreaM2 > 0 ? project.TotalAreaM2 : 0m;
        var requestedStart = request.Data.RequestedStartDateUtc ?? DateTime.UtcNow.AddDays(1);
        var slot = await _scheduler.AllocateAsync(workloadM2, requestedStart, cancellationToken);

        var workOrder = new GlassWorkOrder(project.Id, slot.ScheduledStartUtc, slot.ScheduledEndUtc, workloadM2, request.Data.AssignedTeamId);

        var bomLines = await _bomLineRepo.ListByProjectAsync(project.Id, cancellationToken);
        var plan1D = await _cuttingPlanRepo.GetLatestAsync(project.Id, GlassCuttingPlanType.Profile1D, cancellationToken);
        var plan2D = await _cuttingPlanRepo.GetLatestAsync(project.Id, GlassCuttingPlanType.Glass2D, cancellationToken);
        var snapshotJson = BomSnapshotJsonBuilder.Build(bomLines);
        workOrder.CaptureBomSnapshot(snapshotJson, project.GrandTotal, plan1D?.Id, plan2D?.Id);

        await _workOrderRepo.AddAsync(workOrder, cancellationToken);

        project.TransitionTo(GlassProjectStatus.InProduction, _currentUser.UserId ?? Guid.Empty);
        _projectRepo.Update(project);

        _ = link;
        var latestRevision = await _revisionRepo.GetLatestAsync(workOrder.Id, cancellationToken);
        return MapWorkOrder(workOrder, latestRevision);
    }

    internal static GlassWorkOrderDto MapWorkOrder(GlassWorkOrder w, GlassWorkOrderRevision? latestRevision) => new(
        w.Id, w.ProjectId,
        w.ScheduledStartDate, w.ScheduledEndDate,
        w.AssignedTeamId, w.AssignedInstallerUserId,
        w.WorkloadM2, w.Status.ToString(),
        w.RecutCount, w.DefectNotes,
        w.BomSnapshotJson, w.BomSnapshotTotal, w.RevisionCount,
        w.HasOutstandingBlockingRevision,
        latestRevision?.Status,
        latestRevision?.RevisionNumber,
        latestRevision?.DeltaPercent);
}

public class GetWorkOrdersByProjectQueryHandler : IRequestHandler<GetWorkOrdersByProjectQuery, IReadOnlyList<GlassWorkOrderDto>>
{
    private readonly IGlassWorkOrderRepository _repo;
    private readonly IGlassWorkOrderRevisionRepository _revisionRepo;

    public GetWorkOrdersByProjectQueryHandler(IGlassWorkOrderRepository repo, IGlassWorkOrderRevisionRepository revisionRepo)
    {
        _repo = repo;
        _revisionRepo = revisionRepo;
    }

    public async Task<IReadOnlyList<GlassWorkOrderDto>> Handle(GetWorkOrdersByProjectQuery request, CancellationToken cancellationToken)
    {
        var orders = await _repo.ListByProjectAsync(request.ProjectId, cancellationToken);
        var ids = orders.Select(w => w.Id).ToList();
        var latestByWorkOrder = await _revisionRepo.GetLatestByWorkOrderIdsAsync(ids, cancellationToken);
        return orders
            .Select(w => ReleaseToProductionCommandHandler.MapWorkOrder(w, latestByWorkOrder.TryGetValue(w.Id, out var rev) ? rev : null))
            .ToList();
    }
}

public class UpdateWorkOrderStatusCommandHandler : IRequestHandler<UpdateWorkOrderStatusCommand, GlassWorkOrderDto>
{
    private readonly IGlassWorkOrderRepository _repo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassWorkOrderRevisionRepository _revisionRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateWorkOrderStatusCommandHandler(
        IGlassWorkOrderRepository repo,
        IGlassProjectRepository projectRepo,
        IGlassWorkOrderRevisionRepository revisionRepo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _revisionRepo = revisionRepo;
        _currentUser = currentUser;
    }

    public async Task<GlassWorkOrderDto> Handle(UpdateWorkOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _repo.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("WorkOrder");
        if (!Enum.TryParse<GlassWorkOrderStatus>(request.Status, ignoreCase: true, out var status))
        {
            throw new ArgumentException($"Invalid work order status: {request.Status}");
        }
        workOrder.TransitionTo(status);
        _repo.Update(workOrder);

        if (status == GlassWorkOrderStatus.Ready)
        {
            var project = await _projectRepo.GetByIdAsync(workOrder.ProjectId, cancellationToken);
            if (project is not null && project.Status == GlassProjectStatus.InProduction)
            {
                project.TransitionTo(GlassProjectStatus.Ready, _currentUser.UserId ?? Guid.Empty);
                _projectRepo.Update(project);
            }
        }
        else if (status == GlassWorkOrderStatus.Installed)
        {
            var project = await _projectRepo.GetByIdAsync(workOrder.ProjectId, cancellationToken);
            if (project is not null && project.Status != GlassProjectStatus.Installed)
            {
                project.TransitionTo(GlassProjectStatus.Installed, _currentUser.UserId ?? Guid.Empty);
                _projectRepo.Update(project);
            }
        }

        var latestRevision = await _revisionRepo.GetLatestAsync(workOrder.Id, cancellationToken);
        return ReleaseToProductionCommandHandler.MapWorkOrder(workOrder, latestRevision);
    }
}

public class RecordWorkOrderDefectCommandHandler : IRequestHandler<RecordWorkOrderDefectCommand, GlassWorkOrderDto>
{
    private readonly IGlassWorkOrderRepository _repo;
    private readonly IGlassWorkOrderRevisionRepository _revisionRepo;

    public RecordWorkOrderDefectCommandHandler(IGlassWorkOrderRepository repo, IGlassWorkOrderRevisionRepository revisionRepo)
    {
        _repo = repo;
        _revisionRepo = revisionRepo;
    }

    public async Task<GlassWorkOrderDto> Handle(RecordWorkOrderDefectCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _repo.GetByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("WorkOrder");
        workOrder.RecordDefect(request.DefectNotes);
        workOrder.TransitionTo(GlassWorkOrderStatus.Defective);
        _repo.Update(workOrder);
        var latestRevision = await _revisionRepo.GetLatestAsync(workOrder.Id, cancellationToken);
        return ReleaseToProductionCommandHandler.MapWorkOrder(workOrder, latestRevision);
    }
}

public class GetNotificationHistoryQueryHandler : IRequestHandler<GetNotificationHistoryQuery, IReadOnlyList<NotificationLogDto>>
{
    private readonly IGlassNotificationLogRepository _repo;

    public GetNotificationHistoryQueryHandler(IGlassNotificationLogRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<NotificationLogDto>> Handle(GetNotificationHistoryQuery request, CancellationToken cancellationToken)
    {
        var logs = await _repo.ListByProjectAsync(request.ProjectId, cancellationToken);
        return logs
            .Select(l => new NotificationLogDto(
                l.Id, l.ProjectId,
                l.EventCode.ToString(), l.Channel.ToString(), l.RecipientKind.ToString(),
                l.RecipientAddress, l.Status.ToString(), l.ProviderMessageId,
                l.CreatedAtUtc, l.UpdatedAtUtc, l.DeliveredAtUtc, l.ReadAtUtc,
                l.ErrorMessage, l.RetryCount))
            .ToList();
    }
}


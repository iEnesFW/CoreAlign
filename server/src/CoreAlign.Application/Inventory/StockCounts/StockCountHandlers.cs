using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Inventory.StockCounts;

public class PlanStockCountHandler : IRequestHandler<PlanStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _counts;
    private readonly IStockItemRepository _stockItems;
    private readonly IWarehouseRepository _warehouses;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public PlanStockCountHandler(
        IStockCountRepository counts,
        IStockItemRepository stockItems,
        IWarehouseRepository warehouses,
        IDocumentSequenceRepository sequences,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow)
    {
        _counts = counts;
        _stockItems = stockItems;
        _warehouses = warehouses;
        _sequences = sequences;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<StockCountDto> Handle(PlanStockCountCommand c, CancellationToken ct)
    {
        var warehouse = await _warehouses.GetByIdAsync(c.WarehouseId, ct)
            ?? throw new NoWarehouseConfiguredException();

        var now = DateTime.UtcNow;
        string number;
        if (!string.IsNullOrWhiteSpace(c.CountNumber))
        {
            if (await _counts.CountNumberExistsAsync(c.CountNumber.Trim(), null, ct))
            {
                throw new DuplicateStockCountNumberException();
            }
            number = c.CountNumber.Trim();
        }
        else
        {
            var seq = await _sequences.GetAsync(DocumentSequenceType.StockCountNumber, ct);
            if (seq is null)
            {
                await _sequences.AddAsync(new DocumentSequence(DocumentSequenceType.StockCountNumber, "SC", now.Year, 1, 5), ct);
                await _uow.SaveChangesAsync(ct);
            }
            number = await _sequences.ConsumeAsync(DocumentSequenceType.StockCountNumber, now, ct);
        }

        var entity = new StockCount(number, warehouse.Id, warehouse.Code, warehouse.Name, now, _currentUser.UserId, c.Notes);

        var snapshot = await _stockItems.GetByWarehouseAsync(warehouse.Id, ct);
        var lines = snapshot.Select(s => new StockCountLine(
            s.ProductId,
            s.Product?.Sku ?? string.Empty,
            s.Product?.Name ?? string.Empty,
            s.OnHand,
            s.AvgCost,
            s.LotId,
            s.Lot?.LotNumber,
            s.BinLocation));
        entity.ReplaceLines(lines);

        await _counts.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return StockCountMapper.ToDto(entity);
    }
}

public class StartStockCountHandler : IRequestHandler<StartStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _counts;
    private readonly IUnitOfWork _uow;

    public StartStockCountHandler(IStockCountRepository counts, IUnitOfWork uow)
    {
        _counts = counts;
        _uow = uow;
    }

    public async Task<StockCountDto> Handle(StartStockCountCommand c, CancellationToken ct)
    {
        var entity = await _counts.GetWithLinesAsync(c.Id, ct) ?? throw new StockCountNotFoundException();
        entity.BeginCounting();
        _counts.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return StockCountMapper.ToDto(entity);
    }
}

public class RecordCountHandler : IRequestHandler<RecordCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _counts;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public RecordCountHandler(IStockCountRepository counts, ICurrentUserAccessor currentUser, IUnitOfWork uow)
    {
        _counts = counts;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<StockCountDto> Handle(RecordCountCommand c, CancellationToken ct)
    {
        var entity = await _counts.GetWithLinesAsync(c.Id, ct) ?? throw new StockCountNotFoundException();
        var userId = _currentUser.UserId;
        foreach (var input in c.Lines)
        {
            entity.RecordLineCount(input.LineId, input.CountedQuantity, userId, input.LineNotes);
        }
        _counts.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return StockCountMapper.ToDto(entity);
    }
}

public class ReconcileStockCountHandler : IRequestHandler<ReconcileStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _counts;
    private readonly IUnitOfWork _uow;

    public ReconcileStockCountHandler(IStockCountRepository counts, IUnitOfWork uow)
    {
        _counts = counts;
        _uow = uow;
    }

    public async Task<StockCountDto> Handle(ReconcileStockCountCommand c, CancellationToken ct)
    {
        var entity = await _counts.GetWithLinesAsync(c.Id, ct) ?? throw new StockCountNotFoundException();
        entity.Reconcile(c.Notes);
        _counts.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return StockCountMapper.ToDto(entity);
    }
}

public class PostStockCountHandler : IRequestHandler<PostStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _counts;
    private readonly IAllocationService _allocation;
    private readonly IStockItemRepository _stockItems;
    private readonly IStockReasonCodeRepository _reasons;
    private readonly IGLPostingOutbox _outbox;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public PostStockCountHandler(
        IStockCountRepository counts,
        IAllocationService allocation,
        IStockItemRepository stockItems,
        IStockReasonCodeRepository reasons,
        IGLPostingOutbox outbox,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow)
    {
        _counts = counts;
        _allocation = allocation;
        _stockItems = stockItems;
        _reasons = reasons;
        _outbox = outbox;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<StockCountDto> Handle(PostStockCountCommand c, CancellationToken ct)
    {
        var entity = await _counts.GetWithLinesAsync(c.Id, ct) ?? throw new StockCountNotFoundException();
        if (entity.Status != StockCountStatus.Reconciliation)
        {
            throw new InvalidStockCountStateException(entity.Status.ToString(), nameof(PostStockCountCommand));
        }

        var reasonId = await ResolveReasonAsync(ct);
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        // A physical count is authoritative. Reconcile to the COUNTED ABSOLUTE
        // against the LIVE warehouse balance at Post time, not the snapshot-time
        // VarianceQuantity frozen at Plan — stock can move (issue/receive) inside
        // the count window, and a blind stale delta would silently lose or invent
        // units. Delta = Counted − live OnHand; the GL scrap value is recomputed
        // from this live delta so the journal matches the real movement.
        var netVarianceCost = 0m;
        foreach (var line in entity.Lines.Where(l => l.CountedQuantity.HasValue))
        {
            var liveItem = await _stockItems.GetAsync(line.ProductId, entity.WarehouseId, line.LotId, ct);
            var liveOnHand = liveItem?.OnHand ?? 0m;
            var delta = Math.Round(line.CountedQuantity!.Value - liveOnHand, 4);
            if (delta == 0m) continue;

            netVarianceCost += Math.Round(delta * line.SnapshotUnitCost, 4);
            await _allocation.AdjustAsync(new StockAdjustmentRequest(
                ProductId: line.ProductId,
                WarehouseId: entity.WarehouseId,
                Delta: delta,
                UnitCost: delta > 0m ? line.SnapshotUnitCost : null,
                SourceDocumentType: StockSourceDocumentType.CycleCount,
                SourceDocumentId: entity.Id,
                ReasonCodeId: reasonId,
                Notes: $"Sayım sapması {entity.CountNumber} / {line.ProductSku}",
                LotId: line.LotId,
                PostedByUserId: userId,
                PositiveMovementType: StockMovementType.CountVariancePositive,
                NegativeMovementType: StockMovementType.CountVarianceNegative), ct);
        }

        var netVariance = Math.Round(netVarianceCost, 4);
        if (netVariance != 0m)
        {
            await _outbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.InventoryScrap,
                entity.Id,
                entity.CountNumber,
                now.Date,
                JournalEntryType.Mahsup,
                $"Sayım sapması {entity.CountNumber}",
                BuildCogsAdjustmentLines(netVariance)), ct);
        }

        entity.MarkPosted(userId);
        _counts.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return StockCountMapper.ToDto(entity);
    }

    private async Task<Guid?> ResolveReasonAsync(CancellationToken ct)
    {
        var list = await _reasons.ListAsync(StockReasonCategory.CycleCount, isActive: true, ct);
        return list.FirstOrDefault()?.Id;
    }

    private static IReadOnlyList<GLPostingLine> BuildCogsAdjustmentLines(decimal netVariance)
    {
        var amount = Math.Abs(netVariance);
        return netVariance > 0m
            ? new[]
            {
                new GLPostingLine(GLPostingKey.Inventory, amount, 0m),
                new GLPostingLine(GLPostingKey.CostOfGoodsSold, 0m, amount),
            }
            : new[]
            {
                new GLPostingLine(GLPostingKey.CostOfGoodsSold, amount, 0m),
                new GLPostingLine(GLPostingKey.Inventory, 0m, amount),
            };
    }
}

public class CancelStockCountHandler : IRequestHandler<CancelStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _counts;
    private readonly IUnitOfWork _uow;

    public CancelStockCountHandler(IStockCountRepository counts, IUnitOfWork uow)
    {
        _counts = counts;
        _uow = uow;
    }

    public async Task<StockCountDto> Handle(CancelStockCountCommand c, CancellationToken ct)
    {
        var entity = await _counts.GetWithLinesAsync(c.Id, ct) ?? throw new StockCountNotFoundException();
        entity.Cancel();
        _counts.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return StockCountMapper.ToDto(entity);
    }
}

public class GetStockCountByIdHandler : IRequestHandler<GetStockCountByIdQuery, StockCountDto?>
{
    private readonly IStockCountRepository _counts;
    public GetStockCountByIdHandler(IStockCountRepository counts) => _counts = counts;

    public async Task<StockCountDto?> Handle(GetStockCountByIdQuery q, CancellationToken ct)
    {
        var entity = await _counts.GetWithLinesAsync(q.Id, ct);
        return entity is null ? null : StockCountMapper.ToDto(entity);
    }
}

public class SearchStockCountsHandler : IRequestHandler<SearchStockCountsQuery, PagedResult<StockCountDto>>
{
    private readonly IStockCountRepository _counts;
    public SearchStockCountsHandler(IStockCountRepository counts) => _counts = counts;

    public async Task<PagedResult<StockCountDto>> Handle(SearchStockCountsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _counts.SearchAsync(q.WarehouseId, q.Status, page, pageSize, ct);
        return new PagedResult<StockCountDto>
        {
            Items = items.Select(StockCountMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

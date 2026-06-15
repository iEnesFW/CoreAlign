using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Accounting.Mapping;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.Handlers;

public class CreateAccountingPeriodHandler : IRequestHandler<CreateAccountingPeriodCommand, AccountingPeriodDto>
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;
    public CreateAccountingPeriodHandler(IAccountingPeriodRepository periods, IUnitOfWork uow) { _periods = periods; _uow = uow; }

    public async Task<AccountingPeriodDto> Handle(CreateAccountingPeriodCommand c, CancellationToken ct)
    {
        var existing = await _periods.GetByMonthAsync(c.Year, c.Month, ct);
        if (existing is not null) return AccountingMapper.ToDto(existing);

        var period = new AccountingPeriod(c.Year, c.Month);
        await _periods.AddAsync(period, ct);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(period);
    }
}

public class ClosePeriodHandler : IRequestHandler<ClosePeriodCommand, AccountingPeriodDto>
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;
    public ClosePeriodHandler(IAccountingPeriodRepository periods, IUnitOfWork uow) { _periods = periods; _uow = uow; }

    public async Task<AccountingPeriodDto> Handle(ClosePeriodCommand c, CancellationToken ct)
    {
        var period = await _periods.GetByIdAsync(c.Id, ct) ?? throw new AccountingPeriodNotFoundException(c.Id);
        period.Close(c.ClosedByUserId ?? Guid.Empty, c.Notes);
        _periods.Update(period);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(period);
    }
}

public class ReopenPeriodHandler : IRequestHandler<ReopenPeriodCommand, AccountingPeriodDto>
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;
    public ReopenPeriodHandler(IAccountingPeriodRepository periods, IUnitOfWork uow) { _periods = periods; _uow = uow; }

    public async Task<AccountingPeriodDto> Handle(ReopenPeriodCommand c, CancellationToken ct)
    {
        var period = await _periods.GetByIdAsync(c.Id, ct) ?? throw new AccountingPeriodNotFoundException(c.Id);
        period.Reopen(c.ReopenedByUserId ?? Guid.Empty);
        _periods.Update(period);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(period);
    }
}

public class LockPeriodHandler : IRequestHandler<LockPeriodCommand, AccountingPeriodDto>
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IUnitOfWork _uow;
    public LockPeriodHandler(IAccountingPeriodRepository periods, IUnitOfWork uow) { _periods = periods; _uow = uow; }

    public async Task<AccountingPeriodDto> Handle(LockPeriodCommand c, CancellationToken ct)
    {
        var period = await _periods.GetByIdAsync(c.Id, ct) ?? throw new AccountingPeriodNotFoundException(c.Id);
        period.Lock(c.LockedByUserId ?? Guid.Empty);
        _periods.Update(period);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(period);
    }
}

public class GetAccountingPeriodByIdHandler : IRequestHandler<GetAccountingPeriodByIdQuery, AccountingPeriodDto?>
{
    private readonly IAccountingPeriodRepository _periods;
    public GetAccountingPeriodByIdHandler(IAccountingPeriodRepository periods) => _periods = periods;
    public async Task<AccountingPeriodDto?> Handle(GetAccountingPeriodByIdQuery q, CancellationToken ct)
    {
        var p = await _periods.GetByIdAsync(q.Id, ct)
            ?? throw new AccountingPeriodNotFoundException(q.Id);
        return AccountingMapper.ToDto(p);
    }
}

public class ListAccountingPeriodsHandler : IRequestHandler<ListAccountingPeriodsQuery, IReadOnlyList<AccountingPeriodDto>>
{
    private readonly IAccountingPeriodRepository _periods;
    public ListAccountingPeriodsHandler(IAccountingPeriodRepository periods) => _periods = periods;
    public async Task<IReadOnlyList<AccountingPeriodDto>> Handle(ListAccountingPeriodsQuery q, CancellationToken ct) =>
        (await _periods.ListAsync(q.Year, ct)).Select(AccountingMapper.ToDto).ToList();
}

public class CreateCustomerProductPriceHandler : IRequestHandler<CreateCustomerProductPriceCommand, CustomerProductPriceDto>
{
    private readonly ICustomerProductPriceRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateCustomerProductPriceHandler(ICustomerProductPriceRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<CustomerProductPriceDto> Handle(CreateCustomerProductPriceCommand c, CancellationToken ct)
    {
        var entity = new CustomerProductPrice(
            c.CustomerId, c.ProductId, c.Price, c.Currency,
            c.DiscountPercent, c.MinQuantity, c.MaxQuantity,
            c.ValidFromUtc, c.ValidUntilUtc, c.Notes);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(entity);
    }
}

public class UpdateCustomerProductPriceHandler : IRequestHandler<UpdateCustomerProductPriceCommand, CustomerProductPriceDto>
{
    private readonly ICustomerProductPriceRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCustomerProductPriceHandler(ICustomerProductPriceRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<CustomerProductPriceDto> Handle(UpdateCustomerProductPriceCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new CustomerProductPriceNotFoundException(c.Id);
        entity.Update(c.Price, c.Currency, c.DiscountPercent, c.MinQuantity, c.MaxQuantity, c.ValidFromUtc, c.ValidUntilUtc, c.Notes, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return AccountingMapper.ToDto(entity);
    }
}

public class DeleteCustomerProductPriceHandler : IRequestHandler<DeleteCustomerProductPriceCommand, bool>
{
    private readonly ICustomerProductPriceRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteCustomerProductPriceHandler(ICustomerProductPriceRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteCustomerProductPriceCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct);
        if (entity is null) return false;
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class GetCustomerProductPricesHandler : IRequestHandler<GetCustomerProductPricesQuery, IReadOnlyList<CustomerProductPriceDto>>
{
    private readonly ICustomerProductPriceRepository _repo;
    public GetCustomerProductPricesHandler(ICustomerProductPriceRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CustomerProductPriceDto>> Handle(GetCustomerProductPricesQuery q, CancellationToken ct)
    {
        if (q.CustomerId.HasValue && q.ProductId.HasValue)
        {
            return (await _repo.GetForCustomerAndProductAsync(q.CustomerId.Value, q.ProductId.Value, ct))
                .Select(AccountingMapper.ToDto).ToList();
        }
        if (q.CustomerId.HasValue)
        {
            return (await _repo.GetByCustomerAsync(q.CustomerId.Value, ct)).Select(AccountingMapper.ToDto).ToList();
        }
        if (q.ProductId.HasValue)
        {
            return (await _repo.GetByProductAsync(q.ProductId.Value, ct)).Select(AccountingMapper.ToDto).ToList();
        }
        return Array.Empty<CustomerProductPriceDto>();
    }
}

public class ResolvePriceHandler : IRequestHandler<ResolvePriceQuery, ResolvedPriceDto>
{
    private readonly IPricingService _pricing;
    public ResolvePriceHandler(IPricingService pricing) => _pricing = pricing;

    public async Task<ResolvedPriceDto> Handle(ResolvePriceQuery q, CancellationToken ct)
    {
        var result = await _pricing.ResolveAsync(
            new PriceResolutionRequest(q.ProductId, q.CustomerId, q.Quantity, DateTime.UtcNow, q.Currency), ct);
        return AccountingMapper.ToDto(result);
    }
}

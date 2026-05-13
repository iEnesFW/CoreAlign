using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Mapping;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListPaymentTermsHandler : IRequestHandler<ListPaymentTermsQuery, IReadOnlyList<PaymentTermDto>>
{
    private readonly IPaymentTermRepository _repo;
    public ListPaymentTermsHandler(IPaymentTermRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<PaymentTermDto>> Handle(ListPaymentTermsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.IsActive, ct)).Select(MasterDataMapper.ToDto).ToList();
}

public class GetPaymentTermByIdHandler : IRequestHandler<GetPaymentTermByIdQuery, PaymentTermDto?>
{
    private readonly IPaymentTermRepository _repo;
    public GetPaymentTermByIdHandler(IPaymentTermRepository repo) => _repo = repo;
    public async Task<PaymentTermDto?> Handle(GetPaymentTermByIdQuery q, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(q.Id, ct);
        return e is null ? null : MasterDataMapper.ToDto(e);
    }
}

public class CreatePaymentTermHandler : IRequestHandler<CreatePaymentTermCommand, PaymentTermDto>
{
    private readonly IPaymentTermRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreatePaymentTermHandler(IPaymentTermRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<PaymentTermDto> Handle(CreatePaymentTermCommand c, CancellationToken ct)
    {
        var entity = new PaymentTerm(c.Code, c.Name, c.NetDays, c.DiscountDays, c.DiscountPercent, c.EndOfMonth, c.Description);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class UpdatePaymentTermHandler : IRequestHandler<UpdatePaymentTermCommand, PaymentTermDto>
{
    private readonly IPaymentTermRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdatePaymentTermHandler(IPaymentTermRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<PaymentTermDto> Handle(UpdatePaymentTermCommand c, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("PaymentTerm not found");
        entity.Update(c.Code, c.Name, c.NetDays, c.DiscountDays, c.DiscountPercent, c.EndOfMonth, c.Description, c.IsActive);
        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MasterDataMapper.ToDto(entity);
    }
}

public class DeletePaymentTermHandler : IRequestHandler<DeletePaymentTermCommand, bool>
{
    private readonly IPaymentTermRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeletePaymentTermHandler(IPaymentTermRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<bool> Handle(DeletePaymentTermCommand c, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(c.Id, ct);
        if (e is null) return false;
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

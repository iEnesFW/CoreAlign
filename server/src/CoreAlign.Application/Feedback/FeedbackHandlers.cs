using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Feedback;

internal static class FeedbackMapper
{
    public static FeedbackTicketDto ToDto(FeedbackTicket f) => new(
        f.Id,
        f.Type,
        f.Title,
        f.Description,
        f.Priority,
        f.Status,
        f.Module,
        f.StepsToReproduce,
        f.PageUrl,
        f.CreatedByName,
        f.AdminResponse,
        f.CreatedAtUtc,
        f.ResolvedAtUtc);
}

public class CreateFeedbackHandler : IRequestHandler<CreateFeedbackCommand, FeedbackTicketDto>
{
    private readonly IFeedbackRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateFeedbackHandler(IFeedbackRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<FeedbackTicketDto> Handle(CreateFeedbackCommand c, CancellationToken ct)
    {
        var ticket = new FeedbackTicket(
            c.Type,
            c.Title.Trim(),
            c.Description.Trim(),
            c.Priority,
            string.IsNullOrWhiteSpace(c.Module) ? null : c.Module.Trim(),
            string.IsNullOrWhiteSpace(c.StepsToReproduce) ? null : c.StepsToReproduce.Trim(),
            string.IsNullOrWhiteSpace(c.PageUrl) ? null : c.PageUrl.Trim(),
            createdByName: string.IsNullOrWhiteSpace(c.CreatedByName) ? null : c.CreatedByName.Trim());
        await _repo.AddAsync(ticket, ct);
        await _uow.SaveChangesAsync(ct);
        return FeedbackMapper.ToDto(ticket);
    }
}

public class UpdateFeedbackStatusHandler : IRequestHandler<UpdateFeedbackStatusCommand, FeedbackTicketDto>
{
    private readonly IFeedbackRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateFeedbackStatusHandler(IFeedbackRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<FeedbackTicketDto> Handle(UpdateFeedbackStatusCommand c, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException("Feedback ticket not found");
        ticket.ChangeStatus(c.Status, c.AdminResponse);
        _repo.Update(ticket);
        await _uow.SaveChangesAsync(ct);
        return FeedbackMapper.ToDto(ticket);
    }
}

public class ListFeedbackHandler : IRequestHandler<ListFeedbackQuery, IReadOnlyList<FeedbackTicketDto>>
{
    private readonly IFeedbackRepository _repo;
    public ListFeedbackHandler(IFeedbackRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<FeedbackTicketDto>> Handle(ListFeedbackQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.Status, q.Type, ct)).Select(FeedbackMapper.ToDto).ToList();
}

public class GetFeedbackByIdHandler : IRequestHandler<GetFeedbackByIdQuery, FeedbackTicketDto?>
{
    private readonly IFeedbackRepository _repo;
    public GetFeedbackByIdHandler(IFeedbackRepository repo) => _repo = repo;

    public async Task<FeedbackTicketDto?> Handle(GetFeedbackByIdQuery q, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(q.Id, ct);
        return ticket is null ? null : FeedbackMapper.ToDto(ticket);
    }
}

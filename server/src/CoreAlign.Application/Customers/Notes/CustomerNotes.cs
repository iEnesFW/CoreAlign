using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Customers.Notes;

public sealed record CustomerNoteDto(Guid Id, string Body, Guid CreatedByUserId, DateTime CreatedAtUtc);

public sealed record AddCustomerNoteCommand(Guid CustomerId, string Body, Guid CreatedByUserId)
    : IRequest<CustomerNoteDto>, ITransactionalRequest;

public sealed record GetCustomerNotesQuery(Guid CustomerId) : IRequest<IReadOnlyList<CustomerNoteDto>>;

public sealed class AddCustomerNoteCommandValidator : AbstractValidator<AddCustomerNoteCommand>
{
    public AddCustomerNoteCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(4000).WithMessage("Validation.NoteTooLong");
    }
}

public sealed class AddCustomerNoteHandler : IRequestHandler<AddCustomerNoteCommand, CustomerNoteDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerNoteRepository _notes;

    public AddCustomerNoteHandler(ICustomerRepository customers, ICustomerNoteRepository notes)
    {
        _customers = customers;
        _notes = notes;
    }

    public async Task<CustomerNoteDto> Handle(AddCustomerNoteCommand request, CancellationToken cancellationToken)
    {
        _ = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var note = new CustomerNote(request.CustomerId, request.CreatedByUserId, request.Body);
        await _notes.AddAsync(note, cancellationToken);
        return new CustomerNoteDto(note.Id, note.Body, note.CreatedByUserId, note.CreatedAtUtc);
    }
}

public sealed class GetCustomerNotesHandler : IRequestHandler<GetCustomerNotesQuery, IReadOnlyList<CustomerNoteDto>>
{
    private const int MaxNotes = 100;

    private readonly ICustomerRepository _customers;
    private readonly ICustomerNoteRepository _notes;

    public GetCustomerNotesHandler(ICustomerRepository customers, ICustomerNoteRepository notes)
    {
        _customers = customers;
        _notes = notes;
    }

    public async Task<IReadOnlyList<CustomerNoteDto>> Handle(GetCustomerNotesQuery request, CancellationToken cancellationToken)
    {
        _ = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var notes = await _notes.GetLatestByCustomerAsync(request.CustomerId, MaxNotes, cancellationToken);
        return notes.Select(n => new CustomerNoteDto(n.Id, n.Body, n.CreatedByUserId, n.CreatedAtUtc)).ToList();
    }
}

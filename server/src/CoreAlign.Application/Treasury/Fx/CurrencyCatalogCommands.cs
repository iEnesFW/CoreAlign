using CoreAlign.Application.Common;
using CoreAlign.Application.Lookups;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Treasury.Fx;

public sealed record UpsertCurrencyCommand(string Code, string Name, string? Symbol, bool IsActive)
    : IRequest<CurrencyDto>, ITransactionalRequest;

public sealed class UpsertCurrencyCommandValidator : AbstractValidator<UpsertCurrencyCommand>
{
    public UpsertCurrencyCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Validation.Required")
            .Matches("^[A-Za-z]{3}$").WithMessage("Validation.CurrencyFormat");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Symbol).MaximumLength(8);
    }
}

// The catalogue derives from the FX feed by default; this is the manual half of that decision —
// an operator adding a currency the bulletin does not publish, renaming one, or switching one off.
public sealed class UpsertCurrencyCommandHandler : IRequestHandler<UpsertCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyCatalog _catalog;

    public UpsertCurrencyCommandHandler(ICurrencyCatalog catalog) => _catalog = catalog;

    public async Task<CurrencyDto> Handle(UpsertCurrencyCommand request, CancellationToken cancellationToken)
    {
        var code = Currency.Normalize(request.Code);
        var existing = (await _catalog.ListAllAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            var created = new Currency(code, request.Name, request.Symbol, request.IsActive);
            await _catalog.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return new CurrencyDto(created.Code, created.Name, created.Symbol, created.IsActive);
        }

        existing.Rename(request.Name, request.Symbol);
        existing.SetActive(request.IsActive);
        _catalog.Update(existing);
        return new CurrencyDto(existing.Code, existing.Name, existing.Symbol, existing.IsActive);
    }
}

public sealed record DeactivateCurrencyCommand(string Code) : IRequest<Unit>, ITransactionalRequest;

// WHY deactivate and not delete: a code is the primary key of every historical amount that used it;
// deleting it would orphan documents and rates. Switching it off removes it from the pickers only.
public sealed class DeactivateCurrencyCommandHandler : IRequestHandler<DeactivateCurrencyCommand, Unit>
{
    private readonly ICurrencyCatalog _catalog;

    public DeactivateCurrencyCommandHandler(ICurrencyCatalog catalog) => _catalog = catalog;

    public async Task<Unit> Handle(DeactivateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var code = Currency.Normalize(request.Code);
        var existing = (await _catalog.ListAllAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new CurrencyNotFoundException(code);

        existing.SetActive(false);
        _catalog.Update(existing);
        return Unit.Value;
    }
}

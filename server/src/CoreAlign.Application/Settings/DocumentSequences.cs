using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Settings;

public record DocumentSequenceDto(
    DocumentSequenceType Type,
    string Prefix,
    int PadLength,
    string? Format,
    int CurrentYear,
    long NextNumber,
    string Preview,
    bool IsConfigured);

public record ListDocumentSequencesQuery : IRequest<IReadOnlyList<DocumentSequenceDto>>;

public record ConfigureDocumentSequenceCommand(
    DocumentSequenceType Type,
    string Prefix,
    int PadLength,
    string? Format,
    long? NextNumber) : IRequest<DocumentSequenceDto>, ITransactionalRequest;

internal static class DocumentSequenceDefaults
{
    public static readonly IReadOnlyDictionary<DocumentSequenceType, string> Prefixes =
        new Dictionary<DocumentSequenceType, string>
        {
            [DocumentSequenceType.CustomerCode] = "CUS",
            [DocumentSequenceType.ProductSku] = "PRD",
            [DocumentSequenceType.OrderNumber] = "ORD",
            [DocumentSequenceType.InvoiceNumber] = "INV",
            [DocumentSequenceType.CreditNoteNumber] = "CN",
            [DocumentSequenceType.DebitNoteNumber] = "DN",
            [DocumentSequenceType.PaymentNumber] = "PAY",
            [DocumentSequenceType.ShipmentNumber] = "SHP",
            [DocumentSequenceType.JournalNumber] = "JRN",
            [DocumentSequenceType.SubscriptionOrderNumber] = "SUB",
            [DocumentSequenceType.QuoteNumber] = "QUO",
        };

    public static string PrefixFor(DocumentSequenceType type) =>
        Prefixes.TryGetValue(type, out var p) ? p : type.ToString();
}

public class ListDocumentSequencesHandler : IRequestHandler<ListDocumentSequencesQuery, IReadOnlyList<DocumentSequenceDto>>
{
    private readonly IDocumentSequenceRepository _repo;
    public ListDocumentSequencesHandler(IDocumentSequenceRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<DocumentSequenceDto>> Handle(ListDocumentSequencesQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var existing = (await _repo.ListAsync(ct)).ToDictionary(d => d.Type);

        return Enum.GetValues<DocumentSequenceType>()
            .Select(type =>
            {
                if (existing.TryGetValue(type, out var seq))
                {
                    return new DocumentSequenceDto(type, seq.Prefix, seq.PadLength, seq.Format,
                        seq.CurrentYear, seq.NextNumber, seq.Peek(now), true);
                }
                var preview = new DocumentSequence(type, DocumentSequenceDefaults.PrefixFor(type), now.Year, 1, 5);
                return new DocumentSequenceDto(type, preview.Prefix, preview.PadLength, preview.Format,
                    preview.CurrentYear, preview.NextNumber, preview.Peek(now), false);
            })
            .ToList();
    }
}

public class ConfigureDocumentSequenceHandler : IRequestHandler<ConfigureDocumentSequenceCommand, DocumentSequenceDto>
{
    private readonly IDocumentSequenceRepository _repo;
    private readonly IUnitOfWork _uow;
    public ConfigureDocumentSequenceHandler(IDocumentSequenceRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<DocumentSequenceDto> Handle(ConfigureDocumentSequenceCommand c, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var seq = await _repo.GetAsync(c.Type, ct);
        if (seq is null)
        {
            seq = new DocumentSequence(c.Type, c.Prefix.Trim(), now.Year, c.NextNumber ?? 1,
                c.PadLength < 1 ? 1 : c.PadLength, string.IsNullOrWhiteSpace(c.Format) ? null : c.Format.Trim());
            await _repo.AddAsync(seq, ct);
        }
        else
        {
            seq.UpdateConfig(c.Prefix, c.PadLength, c.Format, c.NextNumber);
            _repo.Update(seq);
        }
        await _uow.SaveChangesAsync(ct);
        return new DocumentSequenceDto(seq.Type, seq.Prefix, seq.PadLength, seq.Format,
            seq.CurrentYear, seq.NextNumber, seq.Peek(now), true);
    }
}

public class ConfigureDocumentSequenceCommandValidator : AbstractValidator<ConfigureDocumentSequenceCommand>
{
    public ConfigureDocumentSequenceCommandValidator()
    {
        RuleFor(x => x.Prefix).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PadLength).InclusiveBetween(1, 12);
        RuleFor(x => x.Format).MaximumLength(60);
        RuleFor(x => x.NextNumber).GreaterThanOrEqualTo(1).When(x => x.NextNumber.HasValue);
    }
}

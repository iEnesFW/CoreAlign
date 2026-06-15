using CoreAlign.Application.Tax.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tax.Handlers;

public class BuildBaBsForPeriodCommandHandler : IRequestHandler<BuildBaBsForPeriodCommand, Guid>
{
    private readonly ITaxDeclarationRepository _declarationRepository;
    private readonly ITaxAggregationRepository _aggregationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TaxOptions _options;

    public BuildBaBsForPeriodCommandHandler(
        ITaxDeclarationRepository declarationRepository,
        ITaxAggregationRepository aggregationRepository,
        IUnitOfWork unitOfWork,
        IOptions<TaxOptions> options)
    {
        _declarationRepository = declarationRepository;
        _aggregationRepository = aggregationRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task<Guid> Handle(BuildBaBsForPeriodCommand request, CancellationToken cancellationToken)
    {
        var startUtc = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endExclusiveUtc = startUtc.AddMonths(1);
        var threshold = _options.BaBsThresholdTry;

        var salesRows = await _aggregationRepository.GetCustomerInvoiceAggregatesAsync(
            startUtc, endExclusiveUtc, threshold, cancellationToken);

        var purchaseRows = await _aggregationRepository.GetVendorBillAggregatesAsync(
            startUtc, endExclusiveUtc, threshold, cancellationToken);

        var salesAggregates = salesRows
            .Select(r => new BaBsCounterpartyAggregate(
                r.TaxNumber,
                r.CustomerName,
                r.DocumentCount,
                r.TotalAmount,
                r.TaxAmount))
            .ToList();

        var purchaseAggregates = purchaseRows
            .Select(r => new BaBsCounterpartyAggregate(
                r.TaxNumber,
                r.VendorName,
                r.DocumentCount,
                r.TotalAmount,
                r.TaxAmount))
            .ToList();

        var babs = new BaBsAggregates(salesAggregates, purchaseAggregates);
        var xmlDoc = BaBsXmlBuilder.Build(request.Year, request.Month, babs);
        var xml = xmlDoc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);

        var totalAmount = salesAggregates.Sum(s => s.TotalAmount) + purchaseAggregates.Sum(p => p.TotalAmount);
        var taxAmount = salesAggregates.Sum(s => s.TaxAmount) + purchaseAggregates.Sum(p => p.TaxAmount);
        var lineCount = salesAggregates.Count + purchaseAggregates.Count;

        var existing = await _declarationRepository.GetByPeriodAsync(
            request.Year, request.Month, TaxDeclarationType.BabsBeyani, cancellationToken);

        if (existing is null)
        {
            existing = new TaxDeclaration(request.Year, request.Month, TaxDeclarationType.BabsBeyani);
            await _declarationRepository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.ReplaceLines(Array.Empty<TaxDeclarationLine>());
        }

        existing.Generate(xml, totalAmount, taxAmount, 0m, lineCount);

        var newLines = new List<TaxDeclarationLine>(lineCount);
        foreach (var s in salesAggregates)
        {
            newLines.Add(new TaxDeclarationLine(
                existing.Id,
                s.TaxNumber,
                $"[Bs] {s.CounterpartyName}",
                s.DocumentCount,
                s.TotalAmount,
                s.TaxAmount));
        }
        foreach (var p in purchaseAggregates)
        {
            newLines.Add(new TaxDeclarationLine(
                existing.Id,
                p.TaxNumber,
                $"[Ba] {p.CounterpartyName}",
                p.DocumentCount,
                p.TotalAmount,
                p.TaxAmount));
        }
        existing.ReplaceLines(newLines);

        _declarationRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.Id;
    }
}

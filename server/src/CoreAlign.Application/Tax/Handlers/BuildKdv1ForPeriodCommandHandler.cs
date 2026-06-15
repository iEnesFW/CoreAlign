using System.Text.Json;
using System.Xml.Linq;
using CoreAlign.Application.Tax.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Tax.Handlers;

public class BuildKdv1ForPeriodCommandHandler : IRequestHandler<BuildKdv1ForPeriodCommand, Guid>
{
    private readonly ITaxDeclarationRepository _declarationRepository;
    private readonly ITaxAggregationRepository _aggregationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BuildKdv1ForPeriodCommandHandler> _logger;

    public BuildKdv1ForPeriodCommandHandler(
        ITaxDeclarationRepository declarationRepository,
        ITaxAggregationRepository aggregationRepository,
        IUnitOfWork unitOfWork,
        ILogger<BuildKdv1ForPeriodCommandHandler> logger)
    {
        _declarationRepository = declarationRepository;
        _aggregationRepository = aggregationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(BuildKdv1ForPeriodCommand request, CancellationToken cancellationToken)
    {
        var startUtc = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endExclusiveUtc = startUtc.AddMonths(1);

        var invoiceRows = await _aggregationRepository.GetInvoiceTaxRowsForPeriodAsync(
            startUtc, endExclusiveUtc, cancellationToken);

        var aggregates = AggregateKdv1(invoiceRows);

        var xmlDoc = Kdv1XmlBuilder.Build(request.Year, request.Month, aggregates);
        var xml = xmlDoc.ToString(SaveOptions.DisableFormatting);

        var existing = await _declarationRepository.GetByPeriodAsync(
            request.Year, request.Month, TaxDeclarationType.Kdv1, cancellationToken);

        if (existing is null)
        {
            existing = new TaxDeclaration(request.Year, request.Month, TaxDeclarationType.Kdv1);
            await _declarationRepository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.ReplaceLines(Array.Empty<TaxDeclarationLine>());
        }

        existing.Generate(
            xml,
            aggregates.TotalTaxableBase,
            aggregates.TotalTaxAmount,
            aggregates.TotalWithholdingAmount,
            invoiceRows.Count);

        _declarationRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.Id;
    }

    private Kdv1Aggregates AggregateKdv1(IReadOnlyList<InvoiceTaxAggregateRow> rows)
    {
        var perRate = new Dictionary<decimal, (decimal Base, decimal Tax)>();
        decimal totalBase = 0m;
        decimal totalTax = 0m;
        decimal totalWithholding = 0m;

        foreach (var row in rows)
        {
            totalBase += row.TaxableTotal;
            totalTax += row.TaxTotal;
            totalWithholding += row.WithholdingTotal;

            if (string.IsNullOrWhiteSpace(row.TaxBreakdownJson))
            {
                continue;
            }

            var entries = ParseBreakdown(row.TaxBreakdownJson, row.InvoiceId);
            if (entries is null)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (!perRate.TryGetValue(entry.Rate, out var current))
                {
                    current = (0m, 0m);
                }
                perRate[entry.Rate] = (current.Base + entry.Base, current.Tax + entry.Tax);
            }
        }

        var breakdown = perRate
            .Select(kv => new TaxRateAggregate(
                kv.Key,
                Math.Round(kv.Value.Base, 4),
                Math.Round(kv.Value.Tax, 4)))
            .ToList();

        return new Kdv1Aggregates(
            Math.Round(totalBase, 4),
            Math.Round(totalTax, 4),
            Math.Round(totalWithholding, 4),
            breakdown);
    }

    private List<(decimal Rate, decimal Base, decimal Tax)>? ParseBreakdown(string json, Guid invoiceId)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            var entries = new List<(decimal Rate, decimal Base, decimal Tax)>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) continue;
                if (!element.TryGetProperty("rate", out var rateProp)) continue;

                var rate = rateProp.GetDecimal();
                var baseAmount = element.TryGetProperty("base", out var baseProp) ? baseProp.GetDecimal() : 0m;
                var amount = element.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0m;
                entries.Add((rate, baseAmount, amount));
            }
            return entries;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Skipping invoice {InvoiceId} with unparseable TaxBreakdownJson.", invoiceId);
            return null;
        }
    }
}

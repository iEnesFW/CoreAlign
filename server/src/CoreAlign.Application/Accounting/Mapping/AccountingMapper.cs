using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Accounting.Mapping;

public static class AccountingMapper
{
    public static AccountingPeriodDto ToDto(AccountingPeriod p) => new()
    {
        Id = p.Id,
        Year = p.Year,
        Month = p.Month,
        Code = p.Code,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Status = p.Status,
        ClosedAtUtc = p.ClosedAtUtc,
        ClosedByUserId = p.ClosedByUserId,
        ReopenedAtUtc = p.ReopenedAtUtc,
        Notes = p.Notes,
    };

    public static CustomerProductPriceDto ToDto(CustomerProductPrice p) => new()
    {
        Id = p.Id,
        CustomerId = p.CustomerId,
        CustomerName = p.Customer?.Name ?? string.Empty,
        ProductId = p.ProductId,
        ProductSku = p.Product?.Sku ?? string.Empty,
        ProductName = p.Product?.Name ?? string.Empty,
        Currency = p.Currency,
        Price = p.Price,
        DiscountPercent = p.DiscountPercent,
        MinQuantity = p.MinQuantity,
        MaxQuantity = p.MaxQuantity,
        ValidFromUtc = p.ValidFromUtc,
        ValidUntilUtc = p.ValidUntilUtc,
        Notes = p.Notes,
        IsActive = p.IsActive,
    };

    public static JournalLineDto ToDto(JournalLine l) => new()
    {
        Id = l.Id,
        LineNumber = l.LineNumber,
        AccountId = l.AccountId,
        AccountCode = l.AccountCode,
        AccountName = l.AccountName,
        Debit = l.Debit,
        Credit = l.Credit,
        Currency = l.Currency,
        Description = l.Description,
        CostCenter = l.CostCenter,
        Project = l.Project,
        ForeignAmount = l.ForeignAmount,
        ExchangeRate = l.ExchangeRate,
    };

    public static JournalEntryDto ToDto(JournalEntry j) => new()
    {
        Id = j.Id,
        Number = j.Number,
        EntryDate = j.EntryDate,
        PostingDate = j.PostingDate,
        Type = j.Type,
        Status = j.Status,
        Description = j.Description,
        Reference = j.Reference,
        TotalDebit = j.TotalDebit,
        TotalCredit = j.TotalCredit,
        PostedAtUtc = j.PostedAtUtc,
        ReversedAtUtc = j.ReversedAtUtc,
        ReversalOfId = j.ReversalOfId,
        ReversedById = j.ReversedById,
        Lines = j.Lines.OrderBy(l => l.LineNumber).Select(ToDto).ToList(),
    };

    public static JournalEntrySummaryDto ToDto(Domain.Interfaces.JournalEntrySearchRow r) => new()
    {
        Id = r.Id,
        Number = r.Number,
        EntryDate = r.EntryDate,
        PostingDate = r.PostingDate,
        Type = r.Type,
        Status = r.Status,
        Description = r.Description,
        Reference = r.Reference,
        TotalDebit = r.TotalDebit,
        TotalCredit = r.TotalCredit,
        LineCount = r.LineCount,
    };

    public static GLAccountDto ToDto(GLAccount a, string? parentCode = null) => new()
    {
        Id = a.Id,
        Code = a.Code,
        Name = a.Name,
        Description = a.Description,
        Type = a.Type,
        NormalSide = a.NormalSide,
        ParentId = a.ParentId,
        ParentCode = parentCode,
        Level = a.Level,
        IsPostable = a.IsPostable,
        IsActive = a.IsActive,
        Currency = a.Currency,
    };

    public static ResolvedPriceDto ToDto(PriceResolutionResult r) => new()
    {
        UnitPrice = r.UnitPrice,
        Currency = r.Currency,
        DiscountPercent = r.DiscountPercent,
        Source = r.Source,
        SourceLabel = r.SourceLabel,
        ReferenceListPrice = r.ReferenceListPrice,
        TaxRatePercent = r.TaxRatePercent,
        IsTaxInclusive = r.IsTaxInclusive,
        TaxRateId = r.TaxRateId,
        AppliedRecordId = r.AppliedRecordId,
    };
}

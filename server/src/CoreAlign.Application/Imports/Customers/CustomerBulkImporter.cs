using System.Globalization;
using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Imports.Common;
using CoreAlign.Domain.Enums;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Imports.Customers;

public class CustomerBulkImporter : BulkImporterBase<CustomerImportRow>
{
    private static readonly string[] Headers =
    {
        "Code","Name","LegalName","TradeName","Type","Email","Phone","TaxNumber","TaxOffice","NationalId","DefaultCurrency","CreditLimit","DefaultDiscountPercent","Notes"
    };

    private readonly IValidator<CreateCustomerCommand> _validator;
    private readonly IMediator _mediator;

    public CustomerBulkImporter(
        IBulkImportRowReader reader,
        IBulkImportSessionStore sessions,
        IValidator<CreateCustomerCommand> validator,
        IMediator mediator)
        : base(reader, sessions)
    {
        _validator = validator;
        _mediator = mediator;
    }

    public override string EntityKind => "customers";
    public override IReadOnlyList<string> ColumnHeaders => Headers;

    protected override CustomerImportRow MapRaw(IReadOnlyDictionary<string, string> raw) => new()
    {
        Code = raw.GetValueOrDefault("Code"),
        Name = raw.GetValueOrDefault("Name") ?? string.Empty,
        LegalName = raw.GetValueOrDefault("LegalName"),
        TradeName = raw.GetValueOrDefault("TradeName"),
        Type = string.IsNullOrWhiteSpace(raw.GetValueOrDefault("Type")) ? "Business" : raw["Type"]!,
        Email = raw.GetValueOrDefault("Email"),
        Phone = raw.GetValueOrDefault("Phone"),
        TaxNumber = raw.GetValueOrDefault("TaxNumber"),
        TaxOffice = raw.GetValueOrDefault("TaxOffice"),
        NationalId = raw.GetValueOrDefault("NationalId"),
        DefaultCurrency = string.IsNullOrWhiteSpace(raw.GetValueOrDefault("DefaultCurrency")) ? "TRY" : raw["DefaultCurrency"]!,
        CreditLimit = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("CreditLimit")),
        DefaultDiscountPercent = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("DefaultDiscountPercent")),
        Notes = raw.GetValueOrDefault("Notes")
    };

    protected override async Task<IReadOnlyList<BulkImportRowError>> ValidateRowAsync(
        CustomerImportRow row,
        int rowNumber,
        CancellationToken cancellationToken)
    {
        var command = BuildCommand(row);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (validation.IsValid) return Array.Empty<BulkImportRowError>();
        return validation.Errors
            .Select(e => new BulkImportRowError
            {
                RowNumber = rowNumber,
                Field = e.PropertyName,
                Message = e.ErrorMessage
            })
            .ToList();
    }

    protected override async Task<bool> CommitRowAsync(CustomerImportRow row, CancellationToken cancellationToken)
    {
        var command = BuildCommand(row);
        await _mediator.Send(command, cancellationToken);
        return true;
    }

    private static CreateCustomerCommand BuildCommand(CustomerImportRow row)
    {
        var type = Enum.TryParse<CustomerType>(row.Type, ignoreCase: true, out var parsed) ? parsed : CustomerType.Business;
        return new CreateCustomerCommand(
            Name: row.Name?.Trim() ?? string.Empty,
            Type: type,
            Code: NullIfEmpty(row.Code),
            LegalName: NullIfEmpty(row.LegalName),
            TradeName: NullIfEmpty(row.TradeName),
            NationalId: NullIfEmpty(row.NationalId),
            TaxNumber: NullIfEmpty(row.TaxNumber),
            TaxOffice: NullIfEmpty(row.TaxOffice),
            Email: NullIfEmpty(row.Email),
            Phone: NullIfEmpty(row.Phone),
            DefaultCurrency: string.IsNullOrWhiteSpace(row.DefaultCurrency) ? "TRY" : row.DefaultCurrency.ToUpper(CultureInfo.InvariantCulture),
            CreditLimit: row.CreditLimit,
            DefaultDiscountPercent: row.DefaultDiscountPercent,
            Notes: NullIfEmpty(row.Notes));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

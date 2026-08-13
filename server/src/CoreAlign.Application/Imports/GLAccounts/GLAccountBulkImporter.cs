using System.Globalization;
using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Imports.Common;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Imports.GLAccounts;

public class GLAccountBulkImporter : BulkImporterBase<GLAccountImportRow>
{
    private static readonly string[] Headers =
    {
        "Code","Name","Type","IsPostable","ParentCode","Currency","Description"
    };

    private readonly IValidator<CreateGLAccountCommand> _validator;
    private readonly IMediator _mediator;

    public GLAccountBulkImporter(
        IBulkImportRowReader reader,
        IBulkImportSessionStore sessions,
        IValidator<CreateGLAccountCommand> validator,
        IMediator mediator)
        : base(reader, sessions)
    {
        _validator = validator;
        _mediator = mediator;
    }

    public override string EntityKind => "gl-accounts";
    public override IReadOnlyList<string> ColumnHeaders => Headers;

    protected override GLAccountImportRow MapRaw(IReadOnlyDictionary<string, string> raw) => new()
    {
        Code = raw.GetValueOrDefault("Code") ?? string.Empty,
        Name = raw.GetValueOrDefault("Name") ?? string.Empty,
        Type = string.IsNullOrWhiteSpace(raw.GetValueOrDefault("Type")) ? "Asset" : raw["Type"]!,
        IsPostable = ParsingHelpers.ParseBool(raw.GetValueOrDefault("IsPostable"), fallback: true),
        ParentCode = raw.GetValueOrDefault("ParentCode"),
        Currency = string.IsNullOrWhiteSpace(raw.GetValueOrDefault("Currency")) ? "TRY" : raw["Currency"]!,
        Description = raw.GetValueOrDefault("Description")
    };

    protected override async Task<IReadOnlyList<BulkImportRowError>> ValidateRowAsync(
        GLAccountImportRow row,
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

    protected override async Task<bool> CommitRowAsync(GLAccountImportRow row, CancellationToken cancellationToken)
    {
        await _mediator.Send(BuildCommand(row), cancellationToken);
        return true;
    }

    private static CreateGLAccountCommand BuildCommand(GLAccountImportRow row) => new(
        Code: (row.Code ?? string.Empty).Trim(),
        Name: (row.Name ?? string.Empty).Trim(),
        Type: string.IsNullOrWhiteSpace(row.Type) ? "Asset" : row.Type.Trim(),
        IsPostable: row.IsPostable,
        ParentId: null,
        Currency: string.IsNullOrWhiteSpace(row.Currency) ? "TRY" : row.Currency.ToUpper(CultureInfo.InvariantCulture),
        Description: string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim());
}

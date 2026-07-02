using CoreAlign.Application.BI;
using CoreAlign.Application.Customers.Mapping;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Export;

public sealed record ExportFileDto(byte[] Content, string ContentType, string FileName);

public sealed record ExportCustomersQuery(BIExportFormat Format, string? Search, bool? IsActive)
    : IRequest<ExportFileDto>;

public sealed class ExportCustomersQueryHandler : IRequestHandler<ExportCustomersQuery, ExportFileDto>
{
    private const int MaxRows = 10000;
    private const int PageSize = 500;

    private readonly ICustomerRepository _customers;
    private readonly IReadOnlyDictionary<BIExportFormat, IExportProvider> _providers;

    public ExportCustomersQueryHandler(ICustomerRepository customers, IEnumerable<IExportProvider> providers)
    {
        _customers = customers;
        _providers = providers.ToDictionary(p => p.Format);
    }

    public async Task<ExportFileDto> Handle(ExportCustomersQuery request, CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(request.Format, out var provider))
        {
            throw new UnsupportedExportFormatException(request.Format.ToString());
        }

        var rows = new List<IDictionary<string, object?>>();
        var page = 1;
        while (rows.Count < MaxRows)
        {
            var (items, total) = await _customers.SearchAsync(
                request.Search, request.IsActive, page, PageSize, cancellationToken);

            foreach (var customer in items)
            {
                var dto = CustomerMapper.ToDto(customer);
                rows.Add(new Dictionary<string, object?>
                {
                    ["code"] = dto.Code,
                    ["name"] = dto.Name,
                    ["email"] = dto.Email,
                    ["phone"] = dto.Phone,
                    ["taxNumber"] = dto.TaxNumber,
                    ["currency"] = dto.DefaultCurrency,
                    ["creditLimit"] = dto.CreditLimit,
                    ["currentBalance"] = dto.CurrentBalance,
                    ["overdueAmount"] = dto.OverdueAmount,
                    ["status"] = dto.Status.ToString(),
                    ["createdAtUtc"] = dto.CreatedAtUtc,
                });
            }

            if (items.Count < PageSize || rows.Count >= total) break;
            page++;
        }

        if (rows.Count > MaxRows)
        {
            rows = rows.Take(MaxRows).ToList();
        }

        var columns = new List<BIResultColumnDto>
        {
            new("code", "Code", "string"),
            new("name", "Name", "string"),
            new("email", "Email", "string"),
            new("phone", "Phone", "string"),
            new("taxNumber", "Tax Number", "string"),
            new("currency", "Currency", "string"),
            new("creditLimit", "Credit Limit", "number"),
            new("currentBalance", "Current Balance", "number"),
            new("overdueAmount", "Overdue", "number"),
            new("status", "Status", "string"),
            new("createdAtUtc", "Created", "datetime"),
        };

        var result = new BIResultDto(columns, rows, rows.Count);
        var bytes = await provider.ExportAsync("Customers", result, cancellationToken);

        var (contentType, ext) = request.Format == BIExportFormat.Csv
            ? ("text/csv", "csv")
            : ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx");

        var fileName = $"customers-{DateTime.UtcNow:yyyyMMdd}.{ext}";
        return new ExportFileDto(bytes, contentType, fileName);
    }
}

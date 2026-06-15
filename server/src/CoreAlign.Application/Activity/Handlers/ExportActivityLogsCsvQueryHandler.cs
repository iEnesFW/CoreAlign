using System.Globalization;
using System.Text;
using CoreAlign.Application.Activity.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Activity.Handlers;

public class ExportActivityLogsCsvQueryHandler : IRequestHandler<ExportActivityLogsCsvQuery, byte[]>
{
    private const int MaxRows = 10_000;
    private readonly IActivityLogRepository _repository;

    public ExportActivityLogsCsvQueryHandler(IActivityLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<byte[]> Handle(ExportActivityLogsCsvQuery request, CancellationToken cancellationToken)
    {
        var filter = GetActivityLogsQueryHandler.ToQueryFilter(request.Filter);
        var rows = await _repository.StreamAsync(filter, MaxRows, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("CreatedAtUtc,UserId,Method,Path,StatusCode,DurationMs,IpAddress,TraceId");
        foreach (var row in rows)
        {
            builder.Append(row.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.UserId?.ToString() ?? string.Empty).Append(',');
            builder.Append(CsvEscape(row.Method)).Append(',');
            builder.Append(CsvEscape(row.Path)).Append(',');
            builder.Append(row.StatusCode.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.DurationMs.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(CsvEscape(row.IpAddress ?? string.Empty)).Append(',');
            builder.AppendLine(CsvEscape(row.TraceId ?? string.Empty));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
    }

    public static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        var escaped = value.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }
}

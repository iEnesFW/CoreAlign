using System.Globalization;
using ClosedXML.Excel;
using CoreAlign.Application.Imports;
using CsvHelper;
using CsvHelper.Configuration;

namespace CoreAlign.Infrastructure.Services.Imports;

public class BulkImportRowReader : IBulkImportRowReader
{
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Read(Stream stream, BulkImportFileFormat format)
    {
        return format switch
        {
            BulkImportFileFormat.Csv => ReadCsv(stream),
            BulkImportFileFormat.Xlsx => ReadXlsx(stream),
            _ => throw new NotSupportedException($"Unsupported import format: {format}")
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadCsv(Stream stream)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        while (csv.Read())
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                var value = csv.GetField(header) ?? string.Empty;
                dict[header.Trim()] = value.Trim();
            }
            rows.Add(dict);
        }
        return rows;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadXlsx(Stream stream)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var headerRow = sheet.Row(1);
        var headers = new List<string>();
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var c = 1; c <= lastCol; c++)
        {
            headers.Add(headerRow.Cell(c).GetString().Trim());
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasAny = false;
            for (var c = 1; c <= headers.Count; c++)
            {
                var value = row.Cell(c).GetString().Trim();
                if (!string.IsNullOrEmpty(value)) hasAny = true;
                dict[headers[c - 1]] = value;
            }
            if (hasAny) rows.Add(dict);
        }
        return rows;
    }
}

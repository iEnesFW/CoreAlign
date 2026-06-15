namespace CoreAlign.Application.Imports;

public interface IBulkImportRowReader
{
    IReadOnlyList<IReadOnlyDictionary<string, string>> Read(Stream stream, BulkImportFileFormat format);
}

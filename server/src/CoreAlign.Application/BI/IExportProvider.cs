using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.BI;

public interface IExportProvider
{
    BIExportFormat Format { get; }
    Task<byte[]> ExportAsync(string title, BIResultDto result, CancellationToken cancellationToken);
}

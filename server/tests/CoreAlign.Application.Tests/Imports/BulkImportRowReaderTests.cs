using System.Text;
using CoreAlign.Application.Imports;
using CoreAlign.Infrastructure.Services.Imports;

namespace CoreAlign.Application.Tests.Imports;

public class BulkImportRowReaderTests
{
    [Fact]
    public void Reads_csv_header_and_rows()
    {
        var csv = "Sku,Name,Price\nABC-1,Apple,10\nABC-2,\"Berry, ripe\",20\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var reader = new BulkImportRowReader();

        var rows = reader.Read(stream, BulkImportFileFormat.Csv);

        rows.Should().HaveCount(2);
        rows[0]["Sku"].Should().Be("ABC-1");
        rows[0]["Name"].Should().Be("Apple");
        rows[1]["Name"].Should().Be("Berry, ripe");
    }
}

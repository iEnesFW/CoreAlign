using System.Text;
using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.Validators;
using CoreAlign.Application.Imports;
using CoreAlign.Application.Imports.Customers;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Tests.Imports;

public class CustomerBulkImporterTests
{
    private readonly InMemorySessionStore _sessions = new();
    private readonly IValidator<CreateCustomerCommand> _validator = new CreateCustomerCommandValidator();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task Preview_50_row_csv_with_3_invalid_marks_those_3()
    {
        var csv = BuildCsv(rowCount: 50, invalidRowIndices: new[] { 5, 17, 41 });
        var reader = new FakeReader(csv);
        var importer = new CustomerBulkImporter(reader, _sessions, _validator, _mediator);

        var preview = await importer.PreviewAsync(new MemoryStream(), BulkImportFileFormat.Csv);

        preview.TotalRowCount.Should().Be(50);
        preview.InvalidRowCount.Should().Be(3);
        preview.ValidRowCount.Should().Be(47);
        preview.Rows.Where(r => !r.IsValid)
            .Select(r => r.RowNumber)
            .Should()
            .BeEquivalentTo(new[] { 7, 19, 43 });
    }

    [Fact]
    public async Task Commit_without_skip_invalid_aborts_when_any_row_invalid()
    {
        var reader = new FakeReader(BuildCsv(rowCount: 5, invalidRowIndices: new[] { 2 }));
        var importer = new CustomerBulkImporter(reader, _sessions, _validator, _mediator);

        var preview = await importer.PreviewAsync(new MemoryStream(), BulkImportFileFormat.Csv);
        var commit = await importer.CommitAsync(preview.SessionId, skipInvalidRows: false, default);

        commit.CommittedCount.Should().Be(0);
        commit.AttemptedCount.Should().Be(0);
        commit.SkippedCount.Should().Be(1);
        await _mediator.DidNotReceive().Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commit_with_skip_invalid_commits_47_of_50_when_3_invalid()
    {
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CoreAlign.Application.Customers.DTOs.CustomerDto()));
        var reader = new FakeReader(BuildCsv(rowCount: 50, invalidRowIndices: new[] { 5, 17, 41 }));
        var importer = new CustomerBulkImporter(reader, _sessions, _validator, _mediator);

        var preview = await importer.PreviewAsync(new MemoryStream(), BulkImportFileFormat.Csv);
        var commit = await importer.CommitAsync(preview.SessionId, skipInvalidRows: true, default);

        commit.AttemptedCount.Should().Be(47);
        commit.CommittedCount.Should().Be(47);
        commit.SkippedCount.Should().Be(3);
        await _mediator.Received(47).Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commit_with_all_valid_rows_succeeds_without_skip_flag()
    {
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CoreAlign.Application.Customers.DTOs.CustomerDto()));
        var reader = new FakeReader(BuildCsv(rowCount: 3, invalidRowIndices: Array.Empty<int>()));
        var importer = new CustomerBulkImporter(reader, _sessions, _validator, _mediator);

        var preview = await importer.PreviewAsync(new MemoryStream(), BulkImportFileFormat.Csv);
        var commit = await importer.CommitAsync(preview.SessionId, skipInvalidRows: false, default);

        commit.CommittedCount.Should().Be(3);
        commit.AttemptedCount.Should().Be(3);
    }

    [Fact]
    public async Task Commit_without_skip_rethrows_row_failure_so_outer_transaction_rolls_back()
    {
        var calls = 0;
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                if (calls == 2)
                {
                    throw new InvalidOperationException("duplicate code");
                }
                return Task.FromResult(new CoreAlign.Application.Customers.DTOs.CustomerDto());
            });
        var reader = new FakeReader(BuildCsv(rowCount: 3, invalidRowIndices: Array.Empty<int>()));
        var importer = new CustomerBulkImporter(reader, _sessions, _validator, _mediator);

        var preview = await importer.PreviewAsync(new MemoryStream(), BulkImportFileFormat.Csv);

        var act = async () => await importer.CommitAsync(preview.SessionId, skipInvalidRows: false, default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Commit_with_skip_invalid_swallows_row_failure_and_records_error()
    {
        var calls = 0;
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                if (calls == 2)
                {
                    throw new InvalidOperationException("duplicate code");
                }
                return Task.FromResult(new CoreAlign.Application.Customers.DTOs.CustomerDto());
            });
        var reader = new FakeReader(BuildCsv(rowCount: 3, invalidRowIndices: Array.Empty<int>()));
        var importer = new CustomerBulkImporter(reader, _sessions, _validator, _mediator);

        var preview = await importer.PreviewAsync(new MemoryStream(), BulkImportFileFormat.Csv);
        var commit = await importer.CommitAsync(preview.SessionId, skipInvalidRows: true, default);

        commit.CommittedCount.Should().Be(2);
        commit.AttemptedCount.Should().Be(3);
        commit.Errors.Should().ContainSingle(e => e.Message.Contains("duplicate code"));
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildCsv(int rowCount, IReadOnlyCollection<int> invalidRowIndices)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();
        for (var i = 0; i < rowCount; i++)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Code"] = $"CUST-{i:D4}",
                ["Name"] = invalidRowIndices.Contains(i) ? "" : $"Customer {i}",
                ["Type"] = "Business",
                ["DefaultCurrency"] = "TRY",
                ["CreditLimit"] = "0",
                ["DefaultDiscountPercent"] = "0"
            };
            rows.Add(dict);
        }
        return rows;
    }

    private class FakeReader : IBulkImportRowReader
    {
        private readonly IReadOnlyList<IReadOnlyDictionary<string, string>> _rows;
        public FakeReader(IReadOnlyList<IReadOnlyDictionary<string, string>> rows) => _rows = rows;
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Read(Stream stream, BulkImportFileFormat format) => _rows;
    }

    private class InMemorySessionStore : IBulkImportSessionStore
    {
        private readonly Dictionary<Guid, object> _store = new();

        public Task<Guid> SaveAsync<TRow>(BulkImportPreviewResult<TRow> preview, CancellationToken cancellationToken = default)
        {
            _store[preview.SessionId] = preview;
            return Task.FromResult(preview.SessionId);
        }

        public Task<BulkImportPreviewResult<TRow>?> GetAsync<TRow>(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(sessionId, out var v);
            return Task.FromResult(v as BulkImportPreviewResult<TRow>);
        }

        public Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _store.Remove(sessionId);
            return Task.CompletedTask;
        }
    }
}

using System.Text;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Common.Storage;

public class VirusScanFileStorageTests
{
    private readonly IFileStorage _inner = Substitute.For<IFileStorage>();
    private readonly IVirusScanner _scanner = Substitute.For<IVirusScanner>();

    private VirusScanFileStorage CreateSut() =>
        new(_inner, _scanner, NullLogger<VirusScanFileStorage>.Instance);

    [Fact]
    public async Task SaveAsync_clean_file_persists_through_inner_storage()
    {
        _scanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(VirusScanResult.Clean("Test"));
        _inner.SaveAsync("scope", "file.txt", Arg.Any<Stream>(), "text/plain", Arg.Any<CancellationToken>())
            .Returns(new StoredFile("path", "text/plain", 4, "/path"));
        var sut = CreateSut();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("data"));

        var result = await sut.SaveAsync("scope", "file.txt", content, "text/plain");

        result.RelativePath.Should().Be("path");
        await _scanner.Received(1).ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _inner.Received(1).SaveAsync("scope", "file.txt", Arg.Any<Stream>(), "text/plain", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_infected_file_throws_and_does_not_persist()
    {
        _scanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(VirusScanResult.Infected("Test", "Eicar-Test-Signature"));
        var sut = CreateSut();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("eicar"));

        var act = () => sut.SaveAsync("scope", "evil.txt", content, "text/plain");

        var ex = await act.Should().ThrowAsync<VirusScanRejectedException>();
        ex.Which.ThreatName.Should().Be("Eicar-Test-Signature");
        ex.Which.Provider.Should().Be("Test");
        await _inner.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_non_seekable_stream_is_buffered_for_scan_then_persist()
    {
        _scanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(VirusScanResult.Clean("Test"));
        _inner.SaveAsync("scope", "file.bin", Arg.Any<Stream>(), "application/octet-stream", Arg.Any<CancellationToken>())
            .Returns(new StoredFile("path", "application/octet-stream", 3, "/path"));
        var sut = CreateSut();
        using var nonSeekable = new NonSeekableStream(new byte[] { 1, 2, 3 });

        var result = await sut.SaveAsync("scope", "file.bin", nonSeekable, "application/octet-stream");

        result.Should().NotBeNull();
        await _scanner.Received(1).ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenReadAsync_delegates_to_inner_storage()
    {
        var expected = new MemoryStream();
        _inner.OpenReadAsync("p", Arg.Any<CancellationToken>()).Returns(expected);
        var sut = CreateSut();

        var actual = await sut.OpenReadAsync("p");

        actual.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DeleteAsync_delegates_to_inner_storage()
    {
        _inner.DeleteAsync("p", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.DeleteAsync("p");

        await _inner.Received(1).DeleteAsync("p", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_throws_when_inner_storage_missing()
    {
        var act = () => new VirusScanFileStorage(null!, _scanner, NullLogger<VirusScanFileStorage>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_throws_when_scanner_missing()
    {
        var act = () => new VirusScanFileStorage(_inner, null!, NullLogger<VirusScanFileStorage>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task NoOpVirusScanner_always_returns_clean()
    {
        var scanner = new NoOpVirusScanner();
        using var stream = new MemoryStream(new byte[] { 1, 2 });

        var result = await scanner.ScanAsync(stream);

        result.IsClean.Should().BeTrue();
        result.ThreatName.Should().BeNull();
        result.Provider.Should().Be(NoOpVirusScanner.ProviderName);
    }

    [Theory]
    [InlineData("stream: OK", true, null)]
    [InlineData("stream: Win.Test.EICAR_HDB-1 FOUND", false, "Win.Test.EICAR_HDB-1")]
    public void ClamAv_parser_extracts_threat_name(string response, bool expectedClean, string? expectedThreat)
    {
        var result = ClamAvVirusScanner.ParseResponse(response);

        result.IsClean.Should().Be(expectedClean);
        result.ThreatName.Should().Be(expectedThreat);
    }

    [Fact]
    public void ClamAv_parser_throws_on_unexpected_response()
    {
        var act = () => ClamAvVirusScanner.ParseResponse("WHATEVER");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ClamAv_parser_throws_on_empty_response()
    {
        var act = () => ClamAvVirusScanner.ParseResponse(string.Empty);
        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _underlying;

        public NonSeekableStream(byte[] data) => _underlying = new MemoryStream(data);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => _underlying.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _underlying.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _underlying.Dispose();
            base.Dispose(disposing);
        }
    }
}

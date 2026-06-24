using System.Text;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Infrastructure.Storage;

namespace CoreAlign.Application.Tests.Common.Upload;

public class FileUploadServiceTests
{
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();

    private static byte[] Png() => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

    private void StubSave() =>
        _storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StoredFile(
                $"t/{ci.ArgAt<string>(0)}/{ci.ArgAt<string>(1)}",
                ci.ArgAt<string>(3),
                100,
                $"https://cdn/{ci.ArgAt<string>(1)}"));

    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;
        public NonSeekableStream(byte[] data) => _inner = new MemoryStream(data);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Seekable_source_validates_and_stores()
    {
        StubSave();
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Png());

        var result = await sut.UploadAsync(
            new FileUploadRequest(content, "x.png", "image/png", "product-image", "product-images"));

        result.RelativePath.Should().Contain("product-images");
        result.FileName.Should().EndWith(".png");
        result.PublicUrl.Should().StartWith("https://cdn/");
        await _storage.Received(1).SaveAsync(
            "product-images",
            Arg.Is<string>(n => n.EndsWith(".png")),
            Arg.Any<Stream>(),
            "image/png",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_seekable_source_buffers_validates_and_stores()
    {
        StubSave();
        var sut = new FileUploadService(_storage);
        await using var content = new NonSeekableStream(Png());

        var result = await sut.UploadAsync(
            new FileUploadRequest(content, "x.png", "image/png", "product-image", "product-images"));

        result.FileName.Should().EndWith(".png");
        await _storage.Received(1).SaveAsync(
            "product-images", Arg.Any<string>(), Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_content_that_is_not_a_real_image_without_storing()
    {
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Encoding.ASCII.GetBytes("<?php evil(); ?>"));

        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            sut.UploadAsync(new FileUploadRequest(content, "x.png", "image/png", "product-image", "product-images")));

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_oversized_without_storing()
    {
        var sut = new FileUploadService(_storage);
        var big = new byte[(6 * 1024 * 1024)];
        Png().CopyTo(big, 0);
        using var content = new MemoryStream(big);

        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            sut.UploadAsync(new FileUploadRequest(content, "x.png", "image/png", "product-image", "product-images")));

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Maps_virus_rejection_to_validation_exception()
    {
        _storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StoredFile>(new VirusScanRejectedException("EICAR", "ClamAV")));
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Png());

        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            sut.UploadAsync(new FileUploadRequest(content, "x.png", "image/png", "product-image", "product-images")));
    }

    [Fact]
    public async Task Rejects_invalid_scope_without_storing()
    {
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Png());

        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            sut.UploadAsync(new FileUploadRequest(content, "x.png", "image/png", "product-image", "../escape")));

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stores_clean_svg_logo()
    {
        StubSave();
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"4\" height=\"4\"/></svg>"));

        var result = await sut.UploadAsync(
            new FileUploadRequest(content, "logo.svg", "image/svg+xml", "tenant-logo", "tenant-logos"));

        result.FileName.Should().EndWith(".svg");
        await _storage.Received(1).SaveAsync(
            "tenant-logos", Arg.Any<string>(), Arg.Any<Stream>(), "image/svg+xml", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_scripted_svg_without_storing()
    {
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(document.cookie)</script></svg>"));

        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            sut.UploadAsync(new FileUploadRequest(content, "logo.svg", "image/svg+xml", "tenant-logo", "tenant-logos")));

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_returns_seekable_buffer_without_storing()
    {
        var sut = new FileUploadService(_storage);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("name,email\nAcme,a@b.com\n"));

        using var validated = await sut.ValidateAsync(
            new FileValidationRequest(content, "data.csv", "text/csv", "import"));

        validated.DetectedType.Should().Be(DetectedFileType.Csv);
        validated.Content.CanSeek.Should().BeTrue();
        validated.Content.Position.Should().Be(0);
        using var reader = new StreamReader(validated.Content, leaveOpen: true);
        (await reader.ReadToEndAsync()).Should().Contain("Acme");
        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Common.Upload;

public class FileUploadValidatorTests
{
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
    private static readonly byte[] Garbage = { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 };

    [Fact]
    public void Valid_jpeg_for_attachment_returns_safe_stored_name()
    {
        var name = FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "vacation photo.JPG", "image/jpeg", 2048, Jpeg);

        name.Should().EndWith(".jpg");
        name.Should().NotContain(" ");
        name.Should().NotContain("vacation");
    }

    [Fact]
    public void Jpeg_content_declared_as_png_is_rejected()
    {
        var act = () => FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "spoof.png", "image/png", 2048, Jpeg);

        act.Should().Throw<FileUploadValidationException>();
    }

    [Fact]
    public void Png_content_with_jpg_extension_is_rejected()
    {
        var act = () => FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "spoof.jpg", "image/png", 2048, Png);

        act.Should().Throw<FileUploadValidationException>();
    }

    [Fact]
    public void Unverifiable_content_is_rejected()
    {
        var act = () => FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "payload.png", "image/png", 2048, Garbage);

        act.Should().Throw<FileUploadValidationException>();
    }

    [Fact]
    public void Oversized_file_is_rejected()
    {
        var act = () => FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "big.jpg", "image/jpeg", FileUploadProfiles.Attachment.MaxBytes + 1, Jpeg);

        act.Should().Throw<FileUploadValidationException>();
    }

    [Fact]
    public void Disallowed_extension_is_rejected()
    {
        var act = () => FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "malware.exe", "image/jpeg", 2048, Jpeg);

        act.Should().Throw<FileUploadValidationException>();
    }

    [Fact]
    public void Disallowed_content_type_is_rejected()
    {
        var act = () => FileUploadValidator.Validate(
            FileUploadProfiles.Attachment, "note.jpg", "text/plain", 2048, Jpeg);

        act.Should().Throw<FileUploadValidationException>();
    }

    [Fact]
    public void Svg_is_rejected_for_image_profile_but_accepted_for_logo()
    {
        var svg = System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

        var imageAct = () => FileUploadValidator.Validate(
            FileUploadProfiles.Image, "logo.svg", "image/svg+xml", 1024, svg);
        imageAct.Should().Throw<FileUploadValidationException>();

        var logoName = FileUploadValidator.Validate(
            FileUploadProfiles.Logo, "logo.svg", "image/svg+xml", 1024, svg);
        logoName.Should().EndWith(".svg");
    }
}

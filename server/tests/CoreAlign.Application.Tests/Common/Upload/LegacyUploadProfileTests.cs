using System.Text;
using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Common.Upload;

public class LegacyUploadProfileTests
{
    private static byte[] Png() => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
    private static byte[] Jpeg() => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0 };
    private static byte[] Webp() => new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 };
    private static byte[] Ico() => new byte[] { 0x00, 0x00, 0x01, 0x00, 1, 0, 0, 0 };
    private static byte[] Svg() => Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");
    private static byte[] Zip() => new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0 };
    private static byte[] CsvText() => Encoding.UTF8.GetBytes("name,email\nAcme,a@b.com\n");

    private static byte[] Heif(string brand)
    {
        var b = new byte[16];
        b[4] = 0x66; b[5] = 0x74; b[6] = 0x79; b[7] = 0x70;
        var brandBytes = Encoding.ASCII.GetBytes(brand);
        b[8] = brandBytes[0]; b[9] = brandBytes[1]; b[10] = brandBytes[2]; b[11] = brandBytes[3];
        return b;
    }

    // ---- product-image profile: jpg/jpeg/png/webp incl. image/jpg alias; gif/pdf rejected ----

    [Theory]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpg", "image/jpg")]
    [InlineData("photo.webp", "image/webp")]
    public void Product_image_accepts_supported_types(string fileName, string contentType)
    {
        var header = contentType.Contains("png") ? Png() : contentType.Contains("webp") ? Webp() : Jpeg();
        var name = FileUploadValidator.Validate(FileUploadProfiles.ProductImage, fileName, contentType, header.Length, header);
        name.Should().EndWith(System.IO.Path.GetExtension(fileName));
    }

    [Theory]
    [InlineData("anim.gif", "image/gif")]
    [InlineData("doc.pdf", "application/pdf")]
    public void Product_image_rejects_unsupported_types(string fileName, string contentType)
    {
        var header = Png();
        Assert.Throws<FileUploadValidationException>(() =>
            FileUploadValidator.Validate(FileUploadProfiles.ProductImage, fileName, contentType, header.Length, header));
    }

    // ---- tenant-logo profile: svg + image/jpg alias ----

    [Fact]
    public void Tenant_logo_accepts_svg()
    {
        var header = Svg();
        var name = FileUploadValidator.Validate(FileUploadProfiles.TenantLogo, "brand.svg", "image/svg+xml", header.Length, header);
        name.Should().EndWith(".svg");
    }

    [Fact]
    public void Tenant_logo_accepts_jpg_alias_content_type()
    {
        var header = Jpeg();
        var name = FileUploadValidator.Validate(FileUploadProfiles.TenantLogo, "brand.jpg", "image/jpg", header.Length, header);
        name.Should().EndWith(".jpg");
    }

    // ---- tenant-theme profile: ico (both content types), svg, webp ----

    [Theory]
    [InlineData("image/x-icon")]
    [InlineData("image/vnd.microsoft.icon")]
    public void Tenant_theme_accepts_ico(string contentType)
    {
        var header = Ico();
        var name = FileUploadValidator.Validate(FileUploadProfiles.TenantTheme, "favicon.ico", contentType, header.Length, header);
        name.Should().EndWith(".ico");
    }

    [Fact]
    public void Tenant_theme_accepts_webp()
    {
        var header = Webp();
        var name = FileUploadValidator.Validate(FileUploadProfiles.TenantTheme, "bg.webp", "image/webp", header.Length, header);
        name.Should().EndWith(".webp");
    }

    [Fact]
    public void Tenant_theme_rejects_ico_extension_with_non_ico_content()
    {
        var header = Png();
        Assert.Throws<FileUploadValidationException>(() =>
            FileUploadValidator.Validate(FileUploadProfiles.TenantTheme, "favicon.ico", "image/x-icon", header.Length, header));
    }

    // ---- glass-photo profile: heic/heif (the content-type compatibility regression) ----

    [Theory]
    [InlineData("shot.heic", "image/heic", "heic")]
    [InlineData("shot.heif", "image/heif", "mif1")]
    public void Glass_photo_accepts_heif_family(string fileName, string contentType, string brand)
    {
        var header = Heif(brand);
        var name = FileUploadValidator.Validate(FileUploadProfiles.GlassPhoto, fileName, contentType, header.Length, header);
        name.Should().EndWith(System.IO.Path.GetExtension(fileName));
    }

    [Fact]
    public void Glass_photo_rejects_svg()
    {
        var header = Svg();
        Assert.Throws<FileUploadValidationException>(() =>
            FileUploadValidator.Validate(FileUploadProfiles.GlassPhoto, "x.svg", "image/svg+xml", header.Length, header));
    }

    // ---- import profile: data-file validation (csv text / xlsx zip), lenient content-type ----

    [Fact]
    public void Import_accepts_csv_text()
    {
        var header = CsvText();
        var detected = FileUploadValidator.ValidateDataFile(FileUploadProfiles.Import, "customers.csv", header.Length, header);
        detected.Should().Be(DetectedFileType.Csv);
    }

    [Fact]
    public void Import_accepts_xlsx_zip()
    {
        var header = Zip();
        var detected = FileUploadValidator.ValidateDataFile(FileUploadProfiles.Import, "customers.xlsx", header.Length, header);
        detected.Should().Be(DetectedFileType.Zip);
    }

    [Fact]
    public void Import_accepts_utf16_bom_csv()
    {
        var header = new byte[] { 0xFF, 0xFE, (byte)'a', 0x00, (byte)',', 0x00, (byte)'b', 0x00 };
        var detected = FileUploadValidator.ValidateDataFile(FileUploadProfiles.Import, "unicode.csv", header.Length, header);
        detected.Should().Be(DetectedFileType.Csv);
    }

    [Fact]
    public void Import_rejects_csv_with_binary_content()
    {
        var header = new byte[] { (byte)'a', 0x00, (byte)'b' };
        Assert.Throws<FileUploadValidationException>(() =>
            FileUploadValidator.ValidateDataFile(FileUploadProfiles.Import, "evil.csv", header.Length, header));
    }

    [Fact]
    public void Import_rejects_xlsx_that_is_not_a_zip()
    {
        var header = CsvText();
        Assert.Throws<FileUploadValidationException>(() =>
            FileUploadValidator.ValidateDataFile(FileUploadProfiles.Import, "fake.xlsx", header.Length, header));
    }

    [Fact]
    public void Import_rejects_unsupported_extension()
    {
        var header = Zip();
        Assert.Throws<FileUploadValidationException>(() =>
            FileUploadValidator.ValidateDataFile(FileUploadProfiles.Import, "malware.exe", header.Length, header));
    }
}

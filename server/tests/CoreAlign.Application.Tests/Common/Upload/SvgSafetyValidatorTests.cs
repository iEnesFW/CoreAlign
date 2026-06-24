using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Common.Upload;

public class SvgSafetyValidatorTests
{
    [Fact]
    public void Accepts_clean_svg()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\" fill=\"#fff\"/></svg>";
        SvgSafetyValidator.IsSafe(svg).Should().BeTrue();
        SvgSafetyValidator.EnsureSafe(svg);
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("<svg onload=\"alert(1)\" xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><a href=\"javascript:alert(1)\"><text>x</text></a></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><foreignObject><b onclick=\"x()\">y</b></foreignObject></svg>")]
    [InlineData("<?xml version=\"1.0\"?><!DOCTYPE svg [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><svg>&xxe;</svg>")]
    public void Rejects_active_content(string svg)
    {
        SvgSafetyValidator.IsSafe(svg).Should().BeFalse();
        Assert.Throws<FileUploadValidationException>(() => SvgSafetyValidator.EnsureSafe(svg));
    }
}

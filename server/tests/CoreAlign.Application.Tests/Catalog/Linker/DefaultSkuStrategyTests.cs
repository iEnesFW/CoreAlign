using CoreAlign.Application.Catalog.Linker;

namespace CoreAlign.Application.Tests.Catalog.Linker;

public class DefaultSkuStrategyTests
{
    private readonly ISkuTemplateProvider _templates = Substitute.For<ISkuTemplateProvider>();
    private readonly ISkuTemplateCache _cache;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DefaultSkuStrategyTests()
    {
        _cache = new InMemorySkuTemplateCache();
        _templates.GetForTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SkuTemplateSet.Default));
    }

    [Fact]
    public void Glass_template_emits_default_prefix()
    {
        var sut = new DefaultSkuStrategy(_templates, _cache);

        var sku = sut.BuildSku(new SkuContext(CatalogItemKind.Glass, "T6", null, _tenantId));

        sku.Should().Be("GE-GLASS-T6");
    }

    [Fact]
    public void Hardware_template_preserves_existing_hyphen()
    {
        var sut = new DefaultSkuStrategy(_templates, _cache);

        var sku = sut.BuildSku(new SkuContext(CatalogItemKind.Hardware, "H-100", null, _tenantId));

        sku.Should().Be("GE-HW-H-100");
    }

    [Fact]
    public void Profile_template_preserves_underscore_and_uppercases()
    {
        var sut = new DefaultSkuStrategy(_templates, _cache);

        var sku = sut.BuildSku(new SkuContext(CatalogItemKind.Profile, "ALU_50x80", null, _tenantId));

        sku.Should().Be("GE-PROF-ALU_50X80");
    }

    [Fact]
    public void Custom_tenant_template_overrides_default()
    {
        var customTenant = Guid.NewGuid();
        var custom = new SkuTemplateSet(
            GlassTemplate: "ACME-G-{code}",
            HardwareTemplate: "ACME-H-{code}",
            ProfileTemplate: "ACME-P-{code}",
            MountingTemplate: "ACME-M-{code}",
            ColorTemplate: "ACME-C-{code}",
            ConnectorTemplate: "ACME-CN-{code}");
        _templates.GetForTenantAsync(customTenant, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(custom));
        var sut = new DefaultSkuStrategy(_templates, _cache);

        var sku = sut.BuildSku(new SkuContext(CatalogItemKind.Glass, "T8", null, customTenant));

        sku.Should().Be("ACME-G-T8");
    }

    [Fact]
    public void Special_characters_are_stripped()
    {
        var sut = new DefaultSkuStrategy(_templates, _cache);

        var sku = sut.BuildSku(new SkuContext(CatalogItemKind.Profile, "ALU 50x80!@", null, _tenantId));

        sku.Should().Be("GE-PROF-ALU50X80");
    }

    [Fact]
    public void Code_longer_than_32_chars_is_truncated()
    {
        var sut = new DefaultSkuStrategy(_templates, _cache);
        var longCode = new string('A', 40);

        var sku = sut.BuildSku(new SkuContext(CatalogItemKind.Hardware, longCode, null, _tenantId));

        var expectedCode = new string('A', 32);
        sku.Should().Be($"GE-HW-{expectedCode}");
    }
}

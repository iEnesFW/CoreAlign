using System.Text;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Tests.Common.Storage;

public class StorageProviderRegistrationTests
{
    private sealed class StubTenant : ITenantContext
    {
        public StubTenant(Guid? tenantId) => CurrentTenantId = tenantId;
        public Guid? CurrentTenantId { get; }
        public bool HasTenant => CurrentTenantId.HasValue;
        public Guid RequireTenantId() => CurrentTenantId ?? throw new InvalidOperationException();
        public void EnsureSameTenant(Guid resourceTenantId) { }
        public IDisposable PushScope(Guid tenantId) => new NoopScope();
        private sealed class NoopScope : IDisposable { public void Dispose() { } }
    }

    private static ServiceProvider BuildProvider(string provider, string? root = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITenantContext>(new StubTenant(Guid.NewGuid()));
        services.AddSingleton<IVirusScanner>(new NoOpVirusScanner());

        var configValues = new Dictionary<string, string?>
        {
            [$"{StorageProviderOptions.SectionName}:Provider"] = provider,
            [$"{FileStorageOptions.SectionName}:RootPath"] = root ?? Path.Combine(Path.GetTempPath(), "corealign-tests", Guid.NewGuid().ToString("N")),
            [$"{FileStorageOptions.SectionName}:PublicBaseUrl"] = "/files",
            [$"{FileStorageOptions.SectionName}:MaxBytesPerFile"] = (10 * 1024 * 1024).ToString(),
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        StorageProviderRegistration.AddStorageProvider(services, configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddStorageProvider_defaults_to_local_when_provider_omitted()
    {
        using var sp = BuildProvider(StorageProviderNames.Local);

        using var scope = sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        storage.Should().BeOfType<VirusScanFileStorage>();
    }

    [Fact]
    public void AddStorageProvider_fails_fast_when_s3_selected_but_unimplemented()
    {
        // S3FileStorage is a package-missing stub; selecting it must fail at startup rather
        // than surface a NotSupportedException on the first upload.
        var act = () => BuildProvider(StorageProviderNames.S3);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddStorageProvider_fails_fast_when_azureblob_selected_but_unimplemented()
    {
        var act = () => BuildProvider(StorageProviderNames.AzureBlob);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task LocalProvider_SaveAsync_writes_to_tenant_isolated_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "corealign-tests", Guid.NewGuid().ToString("N"));
        using var sp = BuildProvider(StorageProviderNames.Local, root);

        using var scope = sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var tenantId = scope.ServiceProvider.GetRequiredService<ITenantContext>().CurrentTenantId!.Value;

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        var stored = await storage.SaveAsync("docs", "hello.txt", content, "text/plain");

        stored.RelativePath.Should().StartWith(tenantId.ToString("N") + "/docs/");
        Directory.Exists(Path.Combine(root, tenantId.ToString("N"), "docs")).Should().BeTrue();

        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void S3FileStorage_throws_NotSupported_until_package_added()
    {
        var tenantId = Guid.NewGuid();
        var sut = new S3FileStorage(
            Microsoft.Extensions.Options.Options.Create(new StorageProviderOptions()),
            new StubTenant(tenantId));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var act = () => sut.SaveAsync("scope", "file.bin", stream, "application/octet-stream").GetAwaiter().GetResult();

        act.Should().Throw<NotSupportedException>();
        sut.BuildStorageKey("scope", "file.bin").Should().StartWith(tenantId.ToString("N") + "/scope/");
    }

    [Fact]
    public void AzureBlobFileStorage_resolves_container_per_tenant_when_configured()
    {
        var tenantId = Guid.NewGuid();
        var options = new StorageProviderOptions
        {
            AzureBlob = new AzureBlobStorageOptions { ContainerPerTenant = true }
        };
        var sut = new AzureBlobFileStorage(
            Microsoft.Extensions.Options.Options.Create(options),
            new StubTenant(tenantId));

        sut.ResolveContainerName().Should().Be($"tenant-{tenantId:N}");
    }

    [Fact]
    public void AzureBlobFileStorage_resolves_shared_container_by_default()
    {
        var tenantId = Guid.NewGuid();
        var options = new StorageProviderOptions
        {
            AzureBlob = new AzureBlobStorageOptions { Container = "shared-bucket" }
        };
        var sut = new AzureBlobFileStorage(
            Microsoft.Extensions.Options.Options.Create(options),
            new StubTenant(tenantId));

        sut.ResolveContainerName().Should().Be("shared-bucket");
    }

    [Theory]
    [InlineData("../escape", "file.txt")]
    [InlineData("scope", "../other-tenant/secret.txt")]
    [InlineData("scope", "..\\..\\evil.exe")]
    [InlineData("scope", "name/with/slash.txt")]
    [InlineData("scope", "name:with:colon.txt")]
    public void S3FileStorage_BuildStorageKey_rejects_path_traversal_inputs(string scope, string fileName)
    {
        var sut = new S3FileStorage(
            Microsoft.Extensions.Options.Options.Create(new StorageProviderOptions()),
            new StubTenant(Guid.NewGuid()));

        var act = () => sut.BuildStorageKey(scope, fileName);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("../escape/file.txt")]
    [InlineData("a/../b")]
    [InlineData("name:with:colon")]
    public void S3FileStorage_ResolvePublicUrl_rejects_path_traversal(string relativePath)
    {
        var sut = new S3FileStorage(
            Microsoft.Extensions.Options.Options.Create(new StorageProviderOptions
            {
                S3 = new S3StorageOptions { Bucket = "b" }
            }),
            new StubTenant(Guid.NewGuid()));

        var act = () => sut.ResolvePublicUrl(relativePath);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AzureBlobFileStorage_ResolvePublicUrl_rejects_path_traversal()
    {
        var sut = new AzureBlobFileStorage(
            Microsoft.Extensions.Options.Options.Create(new StorageProviderOptions
            {
                AzureBlob = new AzureBlobStorageOptions { Container = "c" }
            }),
            new StubTenant(Guid.NewGuid()));

        var act = () => sut.ResolvePublicUrl("../escape/file.txt");

        act.Should().Throw<ArgumentException>();
    }
}

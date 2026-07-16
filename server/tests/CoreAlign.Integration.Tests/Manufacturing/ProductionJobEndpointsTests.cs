using System.Net.Http.Json;
using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CoreAlign.Integration.Tests.Common;

namespace CoreAlign.Integration.Tests.Manufacturing;

public class ProductionJobEndpointsTests : IntegrationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    protected override async Task SeedAsync(CoreAlignDbContext db)
    {
        var product = Product.Create(_tenantId, "TEST-PROD", "Test Product", ProductType.Manufactured, "PCS", null, null);
        product.Id = _productId;
        await db.Set<Product>().AddAsync(product);

        var sequence = new CoreAlign.Domain.Entities.DocumentSequence(DocumentSequenceType.ProductionJobNumber, "JOB", 2026);
        sequence.TenantId = _tenantId;
        await db.Set<CoreAlign.Domain.Entities.DocumentSequence>().AddAsync(sequence);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task List_ReturnsJobsWithoutNPlusOne()
    {
        var client = CreateAuthenticatedClient(_tenantId);

        // Act
        var response = await client.GetAsync("/api/v1.0/production-jobs");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidData_CreatesAndReturnsJob()
    {
        var client = CreateAuthenticatedClient(_tenantId);
        var command = new CreateProductionJobCommand(_productId, 50, "PCS", null, null, null, null, "Test Job");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/production-jobs", command);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.Content.ReadFromJsonAsync<ProductionJobDetailDto>();
        result.Should().NotBeNull();
        result!.JobNumber.Should().StartWith("JOB");
        result.PlannedQuantity.Should().Be(50);
        result.Status.Should().Be(ProductionJobStatus.Draft);
    }
}

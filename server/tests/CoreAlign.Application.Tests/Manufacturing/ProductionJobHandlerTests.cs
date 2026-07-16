using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.Handlers;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Manufacturing;

public class ProductionJobHandlerTests
{
    private readonly Mock<IProductionJobRepository> _repoMock;
    private readonly Mock<IProductionRoutingRepository> _routingRepoMock;
    private readonly Mock<IDocumentSequenceRepository> _seqRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ITenantContext> _tenantMock;

    private readonly Guid _tenantId = Guid.NewGuid();

    public ProductionJobHandlerTests()
    {
        _repoMock = new Mock<IProductionJobRepository>();
        _routingRepoMock = new Mock<IProductionRoutingRepository>();
        _seqRepoMock = new Mock<IDocumentSequenceRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _tenantMock = new Mock<ITenantContext>();
        _tenantMock.Setup(u => u.TenantId).Returns(_tenantId);
    }

    [Fact]
    public async Task CreateJob_ValidatesAndSaves()
    {
        _seqRepoMock.Setup(s => s.GetNextNumberAsync(_tenantId, DocumentSequenceType.ProductionJobNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync("JOB-100");

        var handler = new CreateProductionJobCommandHandler(
            _repoMock.Object, _routingRepoMock.Object, _seqRepoMock.Object, _uowMock.Object, _tenantMock.Object);

        var command = new CreateProductionJobCommand(Guid.NewGuid(), 100, "PCS", null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.JobNumber.Should().Be("JOB-100");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<ProductionJob>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

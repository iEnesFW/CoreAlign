using CoreAlign.Application.B2B;
using CoreAlign.Application.Customers.Merge;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Customers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Idempotency;

[Collection(IdempotencyTestCollection.Name)]
public class MergeCustomersIdempotencyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SourceId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerMergeOperationRepository _operations = Substitute.For<ICustomerMergeOperationRepository>();
    private readonly ICustomerMergeReassignmentService _reassignment = Substitute.For<ICustomerMergeReassignmentService>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly MergeCustomersCommandHandler _sut;

    public MergeCustomersIdempotencyTests()
    {
        _currentUser.UserId.Returns(ActorId);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Substitute.For<IUnitOfWorkTransaction>());
        _reassignment.ReassignAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new CustomerMergeCounts(3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        _ledger.GetCurrentBalanceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(0m);
        _sut = new MergeCustomersCommandHandler(
            _customers, _operations, _reassignment, _ledger, _unitOfWork, _tenantContext, _currentUser);
    }

    [Fact]
    public async Task SameOperationId_ReplaysWithoutReExecutingReassignment()
    {
        var operationId = Guid.NewGuid();
        var priorLog = new CustomerMergeLog(operationId, SourceId, TargetId, ActorId, null)
        {
            TenantId = TenantId,
        };
        priorLog.RecordCounts(7, 5, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        _operations.GetByOperationIdAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(priorLog);

        var command = new MergeCustomersCommand(
            operationId,
            SourceId,
            TargetId,
            DateTime.UtcNow,
            DateTime.UtcNow);

        var result = await _sut.Handle(command, default);

        result.OperationId.Should().Be(operationId);
        result.SourceCustomerId.Should().Be(SourceId);
        result.TargetCustomerId.Should().Be(TargetId);
        result.OrdersMoved.Should().Be(7);
        result.InvoicesMoved.Should().Be(5);
        result.PaymentsMoved.Should().Be(3);
        result.ReplayedFromIdempotency.Should().BeTrue();
        await _reassignment.DidNotReceive().ReassignAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _operations.DidNotReceive().AddAsync(Arg.Any<CustomerMergeLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SameOperationId_WithDifferentSourceTarget_Throws409()
    {
        var operationId = Guid.NewGuid();
        var otherSource = Guid.NewGuid();
        var priorLog = new CustomerMergeLog(operationId, otherSource, TargetId, ActorId, null)
        {
            TenantId = TenantId,
        };
        _operations.GetByOperationIdAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(priorLog);

        var command = new MergeCustomersCommand(
            operationId,
            SourceId,
            TargetId,
            DateTime.UtcNow,
            DateTime.UtcNow);

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<CustomerMergeIdempotencyConflictException>();
        await _reassignment.DidNotReceive().ReassignAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _operations.DidNotReceive().AddAsync(Arg.Any<CustomerMergeLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SameOperationId_WithSwappedSourceAndTarget_Throws409()
    {
        var operationId = Guid.NewGuid();
        var priorLog = new CustomerMergeLog(operationId, SourceId, TargetId, ActorId, null)
        {
            TenantId = TenantId,
        };
        _operations.GetByOperationIdAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(priorLog);

        var command = new MergeCustomersCommand(
            operationId,
            TargetId,
            SourceId,
            DateTime.UtcNow,
            DateTime.UtcNow);

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<CustomerMergeIdempotencyConflictException>();
    }

    [Fact]
    public async Task FreshOperationId_ExecutesReassignmentAndPersistsLog()
    {
        var operationId = Guid.NewGuid();
        var source = BuildCustomer(SourceId, "S");
        var target = BuildCustomer(TargetId, "T");
        _operations.GetByOperationIdAsync(operationId, Arg.Any<CancellationToken>()).Returns((CustomerMergeLog?)null);
        _customers.GetByIdAsync(SourceId, Arg.Any<CancellationToken>()).Returns(source);
        _customers.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(target);

        var command = new MergeCustomersCommand(
            operationId,
            SourceId,
            TargetId,
            source.UpdatedAtUtc,
            target.UpdatedAtUtc);

        var result = await _sut.Handle(command, default);

        result.OperationId.Should().Be(operationId);
        result.ReplayedFromIdempotency.Should().BeFalse();
        result.OrdersMoved.Should().Be(3);
        result.InvoicesMoved.Should().Be(2);
        await _reassignment.Received(1).ReassignAsync(SourceId, TargetId, Arg.Any<CancellationToken>());
        await _operations.Received(1).AddAsync(Arg.Any<CustomerMergeLog>(), Arg.Any<CancellationToken>());
    }

    private static Customer BuildCustomer(Guid id, string suffix)
    {
        var customer = new Customer(
            name: $"Customer-{suffix}",
            type: CustomerType.Business,
            code: $"C-{suffix}",
            email: null,
            defaultCurrency: "TRY")
        {
            Id = id,
            TenantId = TenantId,
        };
        return customer;
    }
}

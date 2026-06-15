using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities.Customers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Merge;

public sealed class MergeCustomersCommandHandler : IRequestHandler<MergeCustomersCommand, MergeCustomersResult>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerMergeOperationRepository _operations;
    private readonly ICustomerMergeReassignmentService _reassignment;
    private readonly ICustomerLedgerRepository _ledger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUser;

    public MergeCustomersCommandHandler(
        ICustomerRepository customers,
        ICustomerMergeOperationRepository operations,
        ICustomerMergeReassignmentService reassignment,
        ICustomerLedgerRepository ledger,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUserAccessor currentUser)
    {
        _customers = customers;
        _operations = operations;
        _reassignment = reassignment;
        _ledger = ledger;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<MergeCustomersResult> Handle(MergeCustomersCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceCustomerId == request.TargetCustomerId)
        {
            throw new CustomerMergeSameIdException();
        }

        var existing = await _operations.GetByOperationIdAsync(request.OperationId, cancellationToken);
        if (existing is not null)
        {
            if (existing.SourceCustomerId != request.SourceCustomerId || existing.TargetCustomerId != request.TargetCustomerId)
            {
                throw new CustomerMergeIdempotencyConflictException();
            }
            return BuildResult(existing, replayed: true);
        }

        var source = await _customers.GetByIdAsync(request.SourceCustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenantContext.EnsureSameTenant(source.TenantId);

        var target = await _customers.GetByIdAsync(request.TargetCustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenantContext.EnsureSameTenant(target.TenantId);

        EnsureConcurrencyToken(source.UpdatedAtUtc, request.SourceUpdatedAtUtc);
        EnsureConcurrencyToken(target.UpdatedAtUtc, request.TargetUpdatedAtUtc);

        if (source.IsAnonymized || source.Status == CustomerStatus.Archived)
        {
            throw new CustomerMergeAlreadyArchivedException();
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var counts = await _reassignment.ReassignAsync(source.Id, target.Id, cancellationToken);

        var sourceOverdueSnapshot = source.OverdueAmount;
        source.Archive();
        source.RecalculateBalance(0m, 0m);
        _customers.Update(source);

        var newTargetBalance = await _ledger.GetCurrentBalanceAsync(target.Id, cancellationToken);
        target.RecalculateBalance(newTargetBalance, target.OverdueAmount + sourceOverdueSnapshot);
        _customers.Update(target);

        var actor = _currentUser.UserId;
        var log = new CustomerMergeLog(request.OperationId, source.Id, target.Id, actor, request.Notes)
        {
            TenantId = source.TenantId,
        };
        log.RecordCounts(
            counts.Orders,
            counts.Invoices,
            counts.Payments,
            counts.Addresses,
            counts.Contacts,
            counts.Comments,
            counts.LedgerEntries,
            counts.Transactions,
            counts.TagLinks,
            counts.DealerLinks,
            counts.CustomerUsers,
            counts.Other);
        await _operations.AddAsync(log, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return BuildResult(log, replayed: false);
    }

    private static void EnsureConcurrencyToken(DateTime actualUpdatedAt, DateTime expectedUpdatedAt)
    {
        var actualTicks = new DateTime(actualUpdatedAt.Ticks, DateTimeKind.Utc).Ticks;
        var expectedTicks = new DateTime(expectedUpdatedAt.Ticks, DateTimeKind.Utc).Ticks;
        var deltaTicks = Math.Abs(actualTicks - expectedTicks);
        if (deltaTicks > TimeSpan.FromSeconds(1).Ticks)
        {
            throw new CustomerMergeConcurrencyException();
        }
    }

    private static MergeCustomersResult BuildResult(CustomerMergeLog log, bool replayed) => new()
    {
        OperationId = log.OperationId,
        SourceCustomerId = log.SourceCustomerId,
        TargetCustomerId = log.TargetCustomerId,
        ExecutedAtUtc = log.ExecutedAtUtc,
        OrdersMoved = log.OrdersMoved,
        InvoicesMoved = log.InvoicesMoved,
        PaymentsMoved = log.PaymentsMoved,
        AddressesMoved = log.AddressesMoved,
        ContactsMoved = log.ContactsMoved,
        CommentsMoved = log.CommentsMoved,
        LedgerEntriesMoved = log.LedgerEntriesMoved,
        TransactionsMoved = log.TransactionsMoved,
        TagLinksMoved = log.TagLinksMoved,
        DealerLinksMoved = log.DealerLinksMoved,
        CustomerUsersMoved = log.CustomerUsersMoved,
        OtherRecordsMoved = log.OtherRecordsMoved,
        ReplayedFromIdempotency = replayed,
    };
}

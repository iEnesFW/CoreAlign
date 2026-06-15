using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Warranty;

public class WarrantyContractService : IWarrantyContractService
{
    private readonly IWarrantyContractRepository _repo;

    public WarrantyContractService(IWarrantyContractRepository repo)
    {
        _repo = repo;
    }

    public async Task<WarrantyContract> CreateAsync(
        Guid orderId,
        Guid customerId,
        WarrantyCoverageType coverageType,
        int warrantyMonths,
        string termsJson,
        Guid? productId = null,
        Guid? workOrderId = null,
        Guid? invoiceId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await _repo.CountForNumberSequenceAsync(year, cancellationToken) + 1;
        var number = $"WC-{year}-{sequence:D5}";

        var contract = new WarrantyContract(
            orderId,
            customerId,
            number,
            coverageType,
            DateTime.UtcNow,
            warrantyMonths,
            termsJson,
            productId,
            workOrderId,
            invoiceId,
            notes);

        await _repo.AddAsync(contract, cancellationToken);
        return contract;
    }

    public async Task ActivateAsync(Guid contractId, DateTime startDate, CancellationToken cancellationToken = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warranty contract {contractId} not found.");
        contract.Activate(startDate);
        _repo.Update(contract);
    }

    public async Task ExtendAsync(Guid contractId, int monthsAdded, string? reason, CancellationToken cancellationToken = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warranty contract {contractId} not found.");
        contract.Extend(monthsAdded, reason);
        _repo.Update(contract);
    }

    public async Task CancelAsync(Guid contractId, string reason, CancellationToken cancellationToken = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warranty contract {contractId} not found.");
        contract.Cancel(reason);
        _repo.Update(contract);
    }

    public async Task SuspendAsync(Guid contractId, string? reason, CancellationToken cancellationToken = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warranty contract {contractId} not found.");
        contract.Suspend(reason);
        _repo.Update(contract);
    }

    public async Task ResumeAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warranty contract {contractId} not found.");
        contract.Resume();
        _repo.Update(contract);
    }

    public async Task<bool> CheckValidityAsync(Guid contractId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, cancellationToken);
        return contract is not null && contract.IsValidAtDate(asOfDate);
    }
}

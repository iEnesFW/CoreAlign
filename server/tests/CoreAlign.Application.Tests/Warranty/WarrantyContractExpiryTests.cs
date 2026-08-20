using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;

namespace CoreAlign.Application.Tests.Warranty;

// WarrantyContract.MarkExpired had no caller, so a contract stayed Active in every list and report
// long after its end date and the WarrantyExpired outbox message was never emitted. The daily
// expiry job now closes elapsed contracts; these lock the aggregate side of that contract.
public class WarrantyContractExpiryTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static WarrantyContract Contract(int months = 12) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "WRN-1", WarrantyCoverageType.FullService, Start, months, "{}")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };

    [Fact]
    public void An_elapsed_contract_expires_and_announces_it()
    {
        var contract = Contract();
        contract.ClearDomainEvents();

        contract.MarkExpired(Start.AddMonths(13));

        contract.Status.Should().Be(WarrantyContractStatus.Expired);
        contract.DomainEvents.OfType<WarrantyExpiredEvent>().Should().ContainSingle();
    }

    [Fact]
    public void A_contract_still_inside_its_term_is_untouched()
    {
        var contract = Contract();
        contract.ClearDomainEvents();

        contract.MarkExpired(Start.AddMonths(6));

        contract.Status.Should().Be(WarrantyContractStatus.Active);
        contract.DomainEvents.Should().BeEmpty();
    }

    // The daily sweep runs every day: expiring twice must not announce twice.
    [Fact]
    public void Expiring_an_already_expired_contract_announces_nothing()
    {
        var contract = Contract();
        contract.MarkExpired(Start.AddMonths(13));
        contract.ClearDomainEvents();

        contract.MarkExpired(Start.AddMonths(14));

        contract.Status.Should().Be(WarrantyContractStatus.Expired);
        contract.DomainEvents.Should().BeEmpty();
    }

    // The status was permanently wrong, but the warranty DECISION never trusted it — it compares
    // the dates. This pins that, so closing the status cannot change who is covered.
    [Fact]
    public void Validity_follows_the_dates_before_and_after_the_status_is_closed()
    {
        var contract = Contract();

        contract.IsValidAtDate(Start.AddMonths(6)).Should().BeTrue();
        contract.IsValidAtDate(Start.AddMonths(13)).Should().BeFalse();

        contract.MarkExpired(Start.AddMonths(13));

        contract.IsValidAtDate(Start.AddMonths(6)).Should().BeFalse("an expired contract covers nothing");
    }
}

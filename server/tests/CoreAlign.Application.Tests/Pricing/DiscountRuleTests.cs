using CoreAlign.Application.Pricing.Discounts.Commands;
using CoreAlign.Application.Pricing.Discounts.Handlers;
using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Pricing;

public class DiscountRuleTests
{
    private readonly IPricingDiscountRuleRepository _repo = Substitute.For<IPricingDiscountRuleRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Create_throws_on_duplicate_code()
    {
        _repo.GetByCodeAsync("BLACK-FRIDAY", Arg.Any<CancellationToken>())
            .Returns(new DiscountRule("BLACK-FRIDAY", "x", DiscountRuleScope.Global, DiscountValueType.Percent, 10m));

        var sut = new CreateDiscountRuleHandler(_repo, _uow);
        var act = () => sut.Handle(new CreateDiscountRuleCommand(
            "BLACK-FRIDAY",
            "Black Friday",
            DiscountRuleScope.Global,
            DiscountValueType.Percent,
            15m), default);
        await act.Should().ThrowAsync<DiscountRuleCodeConflictException>();
    }

    [Fact]
    public async Task Create_persists_new_rule()
    {
        _repo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DiscountRule?)null);
        var sut = new CreateDiscountRuleHandler(_repo, _uow);
        var dto = await sut.Handle(new CreateDiscountRuleCommand(
            "WHOLESALE",
            "Wholesale electronics",
            DiscountRuleScope.CustomerGroup,
            DiscountValueType.Percent,
            20m,
            CustomerGroupId: Guid.NewGuid(),
            ProductCategoryId: Guid.NewGuid(),
            ValidUntilUtc: new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)), default);

        dto.Code.Should().Be("WHOLESALE");
        dto.Value.Should().Be(20m);
        await _repo.Received(1).AddAsync(Arg.Any<DiscountRule>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_rejects_percent_over_100()
    {
        var act = () => new DiscountRule("x", "x", DiscountRuleScope.Global, DiscountValueType.Percent, 150m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesContext_customer_group_scope_requires_matching_group()
    {
        var group = Guid.NewGuid();
        var rule = new DiscountRule("CG", "n", DiscountRuleScope.CustomerGroup, DiscountValueType.Percent, 10m,
            customerGroupId: group);

        rule.MatchesContext(group, null, Guid.NewGuid(), 1m, DateTime.UtcNow).Should().BeTrue();
        rule.MatchesContext(Guid.NewGuid(), null, Guid.NewGuid(), 1m, DateTime.UtcNow).Should().BeFalse();
        rule.MatchesContext(null, null, Guid.NewGuid(), 1m, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void MatchesContext_returns_false_when_min_quantity_unmet()
    {
        var rule = new DiscountRule("Q10", "n", DiscountRuleScope.Global, DiscountValueType.Percent, 5m,
            minQuantity: 10m);
        rule.MatchesContext(null, null, Guid.NewGuid(), 5m, DateTime.UtcNow).Should().BeFalse();
        rule.MatchesContext(null, null, Guid.NewGuid(), 10m, DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void MatchesContext_returns_false_outside_validity_window()
    {
        var rule = new DiscountRule("WINDOW", "n", DiscountRuleScope.Global, DiscountValueType.Percent, 10m,
            validFromUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            validUntilUtc: new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));

        rule.MatchesContext(null, null, Guid.NewGuid(), 1m, new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeFalse();
        rule.MatchesContext(null, null, Guid.NewGuid(), 1m, new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeFalse();
        rule.MatchesContext(null, null, Guid.NewGuid(), 1m, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeTrue();
    }

    [Fact]
    public void ApplyTo_caps_percent_at_subtotal()
    {
        var rule = new DiscountRule("P", "n", DiscountRuleScope.Global, DiscountValueType.Percent, 20m);
        rule.ApplyTo(100m).Should().Be(20m);
        rule.ApplyTo(0m).Should().Be(0m);
    }

    [Fact]
    public void ApplyTo_caps_fixed_at_subtotal()
    {
        var rule = new DiscountRule("F", "n", DiscountRuleScope.Global, DiscountValueType.FixedAmount, 50m);
        rule.ApplyTo(30m).Should().Be(30m);
        rule.ApplyTo(100m).Should().Be(50m);
    }
}

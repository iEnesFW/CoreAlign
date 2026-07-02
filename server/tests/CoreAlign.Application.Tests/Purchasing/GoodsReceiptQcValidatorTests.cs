using CoreAlign.Application.Purchasing;

namespace CoreAlign.Application.Tests.Purchasing;

public class GoodsReceiptQcValidatorTests
{
    private readonly ApproveGoodsReceiptQcCommandValidator _approve = new();
    private readonly RejectGoodsReceiptQcCommandValidator _reject = new();

    [Fact]
    public void Approve_requires_grn_id()
    {
        _approve.Validate(new ApproveGoodsReceiptQcCommand(Guid.Empty)).IsValid.Should().BeFalse();
        _approve.Validate(new ApproveGoodsReceiptQcCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reject_requires_grn_id()
    {
        _reject.Validate(new RejectGoodsReceiptQcCommand(Guid.Empty, "x")).IsValid.Should().BeFalse();
        _reject.Validate(new RejectGoodsReceiptQcCommand(Guid.NewGuid(), "x")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reject_reason_over_500_chars_fails()
    {
        var result = _reject.Validate(new RejectGoodsReceiptQcCommand(Guid.NewGuid(), new string('x', 501)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Reject_allows_null_reason()
    {
        _reject.Validate(new RejectGoodsReceiptQcCommand(Guid.NewGuid(), null)).IsValid.Should().BeTrue();
    }
}

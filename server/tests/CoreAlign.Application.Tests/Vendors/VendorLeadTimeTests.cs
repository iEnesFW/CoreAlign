using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Vendors;

public class VendorLeadTimeTests
{
    [Fact]
    public void New_vendor_has_no_lead_time_override_by_default()
    {
        var vendor = new Vendor("Acme", VendorType.Business);

        vendor.DefaultLeadTimeDays.Should().Be(0, "0 means no override — MRP falls back to the product lead time");
    }

    [Fact]
    public void SetDefaultLeadTime_stores_the_supplier_lead_time()
    {
        var vendor = new Vendor("Acme", VendorType.Business);

        vendor.SetDefaultLeadTime(12);

        vendor.DefaultLeadTimeDays.Should().Be(12);
    }

    [Fact]
    public void SetDefaultLeadTime_rejects_a_negative_lead_time()
    {
        var vendor = new Vendor("Acme", VendorType.Business);

        var act = () => vendor.SetDefaultLeadTime(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

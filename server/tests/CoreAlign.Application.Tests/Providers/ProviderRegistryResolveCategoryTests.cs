using System.Reflection;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.BankReconciliation;
using CoreAlign.Application.Providers.CadImport;
using CoreAlign.Application.Providers.Calendar;
using CoreAlign.Application.Providers.CncExport;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.Freight;
using CoreAlign.Application.Providers.LabelPrinter;
using CoreAlign.Application.Providers.LaserMeter;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Providers;

public class ProviderRegistryResolveCategoryTests
{
    public static IEnumerable<object[]> KnownProviderCategories => new[]
    {
        new object[] { typeof(IEFaturaProvider), ProviderCategory.EFatura },
        new object[] { typeof(IPaymentProvider), ProviderCategory.Payment },
        new object[] { typeof(ILaserMeterAdapter), ProviderCategory.LaserMeter },
        new object[] { typeof(ILabelPrinter), ProviderCategory.LabelPrinter },
        new object[] { typeof(ICncExporter), ProviderCategory.CncExport },
        new object[] { typeof(ICadImporter), ProviderCategory.CadImport },
        new object[] { typeof(IFreightTrackingProvider), ProviderCategory.Freight },
        new object[] { typeof(IBankReconciliationProvider), ProviderCategory.BankReconciliation },
        new object[] { typeof(ICalendarProvider), ProviderCategory.Calendar }
    };

    [Theory]
    [MemberData(nameof(KnownProviderCategories))]
    public void ResolveCategory_returns_matching_enum_for_known_provider_type(
        Type providerInterface,
        ProviderCategory expected)
    {
        var category = InvokeResolveCategory(providerInterface);

        category.Should().Be(expected);
    }

    [Fact]
    public void ResolveCategory_throws_invalid_operation_for_unknown_provider_type()
    {
        var act = () => InvokeResolveCategory(typeof(IUnknownProvider));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IUnknownProvider*");
    }

    private static ProviderCategory InvokeResolveCategory(Type providerInterface)
    {
        var registryType = typeof(ProviderRegistry<>).MakeGenericType(providerInterface);
        var method = registryType.GetMethod(
            "ResolveCategory",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        try
        {
            return (ProviderCategory)method.Invoke(null, null)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private interface IUnknownProvider : IExternalProvider
    {
    }
}

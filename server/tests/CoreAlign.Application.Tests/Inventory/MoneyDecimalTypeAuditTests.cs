using System.Reflection;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// Rule 16 forbids float/double for money and quantity. This audit reflects over
/// every persisted domain entity (TenantEntity / Entity subclasses) and fails if
/// any property is typed float or double — those lose precision and corrupt
/// money/stock arithmetic. decimal(18,4) or bigint minor-unit only.
/// </summary>
public class MoneyDecimalTypeAuditTests
{
    private static IEnumerable<Type> DomainEntities()
    {
        var assembly = typeof(StockItem).Assembly;
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(TenantEntity).IsAssignableFrom(t) || typeof(BaseEntity).IsAssignableFrom(t));
    }

    [Fact]
    public void No_domain_entity_property_is_typed_float_or_double()
    {
        var offenders = new List<string>();
        foreach (var type in DomainEntities())
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (t == typeof(float) || t == typeof(double))
                {
                    offenders.Add($"{type.Name}.{prop.Name} ({t.Name})");
                }
            }
        }

        offenders.Should().BeEmpty(
            "money/quantity fields must be decimal(18,4) or bigint minor-unit (rule 16); float/double lose precision");
    }

    [Fact]
    public void No_domain_entity_backing_field_is_typed_float_or_double()
    {
        var offenders = new List<string>();
        foreach (var type in DomainEntities())
        {
            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var t = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                if (t == typeof(float) || t == typeof(double))
                {
                    offenders.Add($"{type.Name}.{field.Name} ({t.Name})");
                }
            }
        }

        offenders.Should().BeEmpty("float/double backing fields on entities are forbidden for money/quantity (rule 16)");
    }

    [Fact]
    public void Stock_and_invoice_money_fields_are_decimal()
    {
        AssertDecimal<StockItem>(nameof(StockItem.OnHand), nameof(StockItem.Reserved), nameof(StockItem.AvgCost));
        AssertDecimal<Invoice>(nameof(Invoice.Subtotal), nameof(Invoice.TaxTotal), nameof(Invoice.Total), nameof(Invoice.AmountPaid));
        AssertDecimal<InvoiceLine>(nameof(InvoiceLine.Quantity), nameof(InvoiceLine.UnitPrice), nameof(InvoiceLine.LineTotal), nameof(InvoiceLine.TaxAmount));
        AssertDecimal<StockMovement>(nameof(StockMovement.Quantity), nameof(StockMovement.UnitCost), nameof(StockMovement.TotalCost));
    }

    private static void AssertDecimal<T>(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            var prop = typeof(T).GetProperty(name);
            prop.Should().NotBeNull($"{typeof(T).Name}.{name} must exist");
            (Nullable.GetUnderlyingType(prop!.PropertyType) ?? prop.PropertyType)
                .Should().Be(typeof(decimal), $"{typeof(T).Name}.{name} holds money/quantity");
        }
    }
}

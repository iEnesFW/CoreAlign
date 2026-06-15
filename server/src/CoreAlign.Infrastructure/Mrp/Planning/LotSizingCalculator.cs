using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public interface ILotSizingCalculator
{
    decimal Calculate(
        MrpProductSnapshot product,
        decimal netRequirement,
        decimal projectedAvailableBeforeReceipt,
        decimal averageDailyDemand,
        IReadOnlyList<decimal> upcomingNetRequirements);
}

public sealed class LotSizingCalculator : ILotSizingCalculator
{
    private const decimal DaysPerYear = 365m;
    private const int DefaultPeriodOrderBuckets = 7;

    public decimal Calculate(
        MrpProductSnapshot product,
        decimal netRequirement,
        decimal projectedAvailableBeforeReceipt,
        decimal averageDailyDemand,
        IReadOnlyList<decimal> upcomingNetRequirements)
    {
        if (netRequirement <= 0m)
        {
            return 0m;
        }

        var baseQuantity = product.LotSizingPolicy switch
        {
            LotSizingPolicy.LotForLot => netRequirement,
            LotSizingPolicy.FixedOrderQuantity => CalculateFixedOrderQuantity(product, netRequirement),
            LotSizingPolicy.MinMax => CalculateMinMax(product, projectedAvailableBeforeReceipt, netRequirement),
            LotSizingPolicy.EconomicOrderQuantity => CalculateEoq(product, netRequirement, averageDailyDemand, projectedAvailableBeforeReceipt),
            LotSizingPolicy.PeriodOrderQuantity => CalculatePeriodOrderQuantity(netRequirement, upcomingNetRequirements),
            _ => netRequirement
        };

        return ApplyPostProcessing(product, baseQuantity);
    }

    private static decimal CalculateFixedOrderQuantity(MrpProductSnapshot product, decimal netRequirement)
    {
        if (product.FixedOrderQuantity <= 0m)
        {
            return netRequirement;
        }
        var multiples = Math.Ceiling(netRequirement / product.FixedOrderQuantity);
        return multiples * product.FixedOrderQuantity;
    }

    private static decimal CalculateMinMax(MrpProductSnapshot product, decimal projectedAvailableBeforeReceipt, decimal netRequirement)
    {
        var reorderPoint = product.ReorderPoint > 0m ? product.ReorderPoint : product.SafetyStock;
        var maxStockTarget = product.MaxStock > 0m
            ? product.MaxStock
            : (reorderPoint > 0m ? reorderPoint * 2m : netRequirement);
        var raiseTo = maxStockTarget - projectedAvailableBeforeReceipt;
        return Math.Max(netRequirement, raiseTo);
    }

    private static decimal CalculateEoq(
        MrpProductSnapshot product,
        decimal netRequirement,
        decimal averageDailyDemand,
        decimal projectedAvailableBeforeReceipt)
    {
        var annualDemand = product.EoqAnnualDemand > 0m
            ? product.EoqAnnualDemand
            : Math.Round(averageDailyDemand * DaysPerYear, 4);
        var holdingCost = product.HoldingCostRate * product.UnitCost;

        if (annualDemand <= 0m || product.OrderingCost <= 0m || holdingCost <= 0m)
        {
            return CalculateMinMax(product, projectedAvailableBeforeReceipt, netRequirement);
        }

        var eoqRaw = Math.Sqrt((double)(2m * annualDemand * product.OrderingCost / holdingCost));
        var eoq = (decimal)Math.Ceiling(eoqRaw);
        if (eoq <= 0m)
        {
            return netRequirement;
        }
        var multiples = Math.Ceiling(netRequirement / eoq);
        return multiples * eoq;
    }

    private static decimal CalculatePeriodOrderQuantity(decimal netRequirement, IReadOnlyList<decimal> upcomingNetRequirements)
    {
        var grouped = netRequirement;
        var count = Math.Min(DefaultPeriodOrderBuckets, upcomingNetRequirements.Count);
        for (var i = 0; i < count; i++)
        {
            grouped += Math.Max(0m, upcomingNetRequirements[i]);
        }
        return grouped;
    }

    private static decimal ApplyPostProcessing(MrpProductSnapshot product, decimal quantity)
    {
        if (quantity <= 0m)
        {
            return 0m;
        }

        if (product.OrderMultiple > 0m)
        {
            var multiples = Math.Ceiling(quantity / product.OrderMultiple);
            quantity = multiples * product.OrderMultiple;
        }

        if (product.MinOrderQuantity is { } moq && moq > 0m && quantity < moq)
        {
            quantity = moq;
            if (product.OrderMultiple > 0m)
            {
                var multiples = Math.Ceiling(quantity / product.OrderMultiple);
                quantity = multiples * product.OrderMultiple;
            }
        }

        return Math.Round(quantity, 4);
    }
}

namespace CoreAlign.Domain.Enums;

// How a product's issued stock is costed (relieved to COGS). WeightedAverage is the default and the
// system's historical behaviour; Fifo and Standard are opt-in per product.
public enum CostingMethod
{
    // Issues at the item's running weighted-average cost (StockItem.AvgCost).
    WeightedAverage = 0,

    // Issues consume the oldest received cost layers first (first-in-first-out).
    Fifo = 1,

    // Issues at the product's fixed StandardCost; the actual-vs-standard difference is booked to a
    // cost-variance account at issue time so inventory (153) still nets at actual.
    Standard = 2,
}

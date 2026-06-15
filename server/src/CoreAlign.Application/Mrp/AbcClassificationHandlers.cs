using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Mrp;

public class ClassifyProductsAbcHandler : IRequestHandler<ClassifyProductsAbcCommand, ClassifyProductsAbcResultDto>
{
    private readonly IAbcUsageDataLoader _usageLoader;
    private readonly IProductRepository _products;

    public ClassifyProductsAbcHandler(IAbcUsageDataLoader usageLoader, IProductRepository products)
    {
        _usageLoader = usageLoader;
        _products = products;
    }

    public async Task<ClassifyProductsAbcResultDto> Handle(ClassifyProductsAbcCommand request, CancellationToken cancellationToken)
    {
        var asOf = DateTime.SpecifyKind(request.AsOfDateUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        var usage = await _usageLoader.LoadAsync(asOf, cancellationToken);
        if (usage.Count == 0)
        {
            return new ClassifyProductsAbcResultDto(0, 0, 0, 0, 0, 0, asOf);
        }

        var productById = usage.ToDictionary(u => u.Product.Id, u => u.Product);

        var classification = AbcClassifier.Classify(
            usage.Select(u => new AbcUsageInput(u.Product.Id, u.AnnualUsageValue)));

        var counts = new Dictionary<AbcClass, int>
        {
            [AbcClass.A] = 0,
            [AbcClass.B] = 0,
            [AbcClass.C] = 0,
            [AbcClass.Unclassified] = 0,
        };
        var policyDefaultsApplied = 0;

        foreach (var result in classification)
        {
            if (!productById.TryGetValue(result.ProductId, out var product))
            {
                continue;
            }

            product.SetAbcClass(result.AbcClass);
            counts[result.AbcClass]++;

            if (HasNoExplicitPlanningOverride(product))
            {
                var policy = AbcClassPolicyDefaults.For(result.AbcClass);
                if (policy.ServiceLevelTarget > 0m)
                {
                    product.SetPlanningPolicy(
                        policy.Policy,
                        product.FixedOrderQuantity,
                        product.OrderMultiple,
                        product.EoqAnnualDemand,
                        product.OrderingCost,
                        product.HoldingCostRate,
                        policy.ServiceLevelTarget);
                    policyDefaultsApplied++;
                }
            }

            _products.Update(product);
        }

        return new ClassifyProductsAbcResultDto(
            usage.Count,
            counts[AbcClass.A],
            counts[AbcClass.B],
            counts[AbcClass.C],
            counts[AbcClass.Unclassified],
            policyDefaultsApplied,
            asOf);
    }

    private static bool HasNoExplicitPlanningOverride(Product product) =>
        product.ServiceLevelTarget == 0m && product.LotSizingPolicy == LotSizingPolicy.MinMax;
}

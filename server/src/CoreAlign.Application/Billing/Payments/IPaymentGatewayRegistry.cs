namespace CoreAlign.Application.Billing.Payments;

/// <summary>
/// Resolves a registered <see cref="IPaymentGateway"/> by <see cref="IPaymentGateway.Name"/>.
/// Implementations should be safe to call concurrently and from any tenant scope.
/// </summary>
public interface IPaymentGatewayRegistry
{
    /// <summary>Returns the gateway with the given name, or null if none registered.</summary>
    IPaymentGateway? Find(string name);

    /// <summary>Returns the gateway with the given name, throwing if missing.</summary>
    IPaymentGateway Require(string name);

    /// <summary>All registered gateway names; ordered by registration.</summary>
    IReadOnlyList<string> Names { get; }
}

public sealed class PaymentGatewayRegistry : IPaymentGatewayRegistry
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _byName;

    public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways)
    {
        ArgumentNullException.ThrowIfNull(gateways);
        var dict = new Dictionary<string, IPaymentGateway>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (var g in gateways)
        {
            if (string.IsNullOrWhiteSpace(g.Name)) throw new InvalidOperationException($"Gateway {g.GetType().FullName} has no Name.");
            if (dict.ContainsKey(g.Name)) throw new InvalidOperationException($"Duplicate payment gateway name: '{g.Name}'.");
            dict[g.Name] = g;
            ordered.Add(g.Name);
        }
        _byName = dict;
        Names = ordered;
    }

    public IPaymentGateway? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _byName.TryGetValue(name, out var g) ? g : null;
    }

    public IPaymentGateway Require(string name) =>
        Find(name) ?? throw new InvalidOperationException($"Payment gateway '{name}' is not registered.");

    public IReadOnlyList<string> Names { get; }
}

namespace CoreAlign.Domain.Exceptions;

public class ModuleNotFoundException : NotFoundException
{
    public ModuleNotFoundException() : base("Module not found.") { }
    public ModuleNotFoundException(string code) : base($"Module '{code}' not found.") { }
}

public class ModulePricePlanNotFoundException : NotFoundException
{
    public ModulePricePlanNotFoundException() : base("Module price plan not found.") { }
}

public class SubscriptionOrderNotFoundException : NotFoundException
{
    public SubscriptionOrderNotFoundException() : base("Subscription order not found.") { }
}

public class SubscriptionOrderInvalidStateException : ConflictException
{
    public SubscriptionOrderInvalidStateException(string message) : base(message) { }
}

public class SubscriptionOrderForbiddenException : ForbiddenException
{
    public SubscriptionOrderForbiddenException() : base("You cannot operate on this subscription order.") { }
}

public class PaymentGatewayNotConfiguredException : ConflictException
{
    public PaymentGatewayNotConfiguredException(string gatewayName) : base($"Payment gateway '{gatewayName}' is not configured.") { }
    public PaymentGatewayNotConfiguredException() : base("No default payment gateway is configured for this environment.") { }
}

public class ModuleNotActiveForTenantException : ForbiddenException
{
    public ModuleNotActiveForTenantException(string code) : base($"Module '{code}' is not active for the current tenant.") { }
}

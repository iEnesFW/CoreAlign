namespace CoreAlign.Domain.Exceptions;

public class MrpPlanRunNotFoundException : NotFoundException
{
    public MrpPlanRunNotFoundException() : base("MRP plan run not found.") { }
    public MrpPlanRunNotFoundException(Guid id) : base($"MRP plan run {id} not found.") { }
}

public class MrpPlannedOrderNotFoundException : NotFoundException
{
    public MrpPlannedOrderNotFoundException() : base("MRP planned order not found.") { }
    public MrpPlannedOrderNotFoundException(Guid id) : base($"MRP planned order {id} not found.") { }
}

public class MrpActionMessageNotFoundException : NotFoundException
{
    public MrpActionMessageNotFoundException() : base("MRP action message not found.") { }
    public MrpActionMessageNotFoundException(Guid id) : base($"MRP action message {id} not found.") { }
}

public class MrpPlannedOrderAlreadyReleasedException : ConflictException
{
    public MrpPlannedOrderAlreadyReleasedException(Guid id)
        : base($"MRP planned order {id} has already been released and cannot be modified.") { }
}

public class PlannedProductionOrderNotFoundException : NotFoundException
{
    public PlannedProductionOrderNotFoundException() : base("Planned production order not found.") { }
    public PlannedProductionOrderNotFoundException(Guid id) : base($"Planned production order {id} not found.") { }
}

public class InvalidPlannedProductionOrderTransitionException : ConflictException
{
    public InvalidPlannedProductionOrderTransitionException(string fromStatus, string toStatus)
        : base($"Cannot transition planned production order from {fromStatus} to {toStatus}.") { }
}

public class WarehouseNotFoundException : NotFoundException
{
    public WarehouseNotFoundException() : base("No warehouse is available to receive production.") { }
    public WarehouseNotFoundException(Guid id) : base($"Warehouse {id} not found.") { }
}

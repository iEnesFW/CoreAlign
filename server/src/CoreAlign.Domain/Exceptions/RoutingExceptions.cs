namespace CoreAlign.Domain.Exceptions;

public class RoutingNotFoundException : NotFoundException
{
    public RoutingNotFoundException() : base("Production routing not found.") { }
    public RoutingNotFoundException(Guid id) : base($"Production routing {id} not found.") { }
}

public class WorkCenterOperatorNotFoundException : NotFoundException
{
    public WorkCenterOperatorNotFoundException() : base("Work center operator not found.") { }
    public WorkCenterOperatorNotFoundException(Guid id)
        : base($"Work center operator {id} not found.") { }
}

public class InvalidRoutingTransitionException : ConflictException
{
    public InvalidRoutingTransitionException(string from, string to)
        : base($"A production routing cannot move from '{from}' to '{to}'.") { }
}

public class RoutingHasNoStepsException : ConflictException
{
    public RoutingHasNoStepsException()
        : base("A production routing must have at least one step before it can be activated.") { }
}

public class RoutingNotEditableException : ConflictException
{
    public RoutingNotEditableException()
        : base("A production routing can only be edited while it is in Draft status.") { }
}

public class RoutingNotActiveException : ConflictException
{
    public RoutingNotActiveException()
        : base("Only an active production routing can be assigned to a product.") { }
}

public class RoutingCodeConflictException : ConflictException
{
    public RoutingCodeConflictException(string code)
        : base($"A production routing with code '{code}' already exists.") { }
}

public class WorkCenterNotFoundException : NotFoundException
{
    public WorkCenterNotFoundException() : base("Work center not found.") { }
    public WorkCenterNotFoundException(Guid id) : base($"Work center {id} not found.") { }
}

public class WorkCenterCodeConflictException : ConflictException
{
    public WorkCenterCodeConflictException(string code)
        : base($"A work center with code '{code}' already exists.") { }
}

public class RoutingNotDeletableException : ConflictException
{
    public RoutingNotDeletableException()
        : base("Only a Draft production routing can be deleted; archive active routings instead.") { }
}

public class WorkCenterOperatorAlreadyAssignedException : ConflictException
{
    public WorkCenterOperatorAlreadyAssignedException()
        : base("This operator is already assigned to the work center.") { }
}

public class DuplicateRoutingStepException : ConflictException
{
    public DuplicateRoutingStepException(int stepNumber)
        : base($"Routing step number {stepNumber} is duplicated.") { }
}

public class RoutingStepsNotSequentialException : ConflictException
{
    public RoutingStepsNotSequentialException()
        : base("Routing step numbers must form a gapless ascending sequence starting at 1.") { }
}

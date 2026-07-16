namespace CoreAlign.Domain.Exceptions;

public class ProductionJobNotFoundException : NotFoundException
{
    public ProductionJobNotFoundException() : base("Production job not found.") { }
    public ProductionJobNotFoundException(Guid id) : base($"Production job {id} not found.") { }
}

public class InvalidProductionJobTransitionException : ConflictException
{
    public InvalidProductionJobTransitionException(string from, string to)
        : base($"A production job cannot move from '{from}' to '{to}'.") { }
}

public class InvalidProductionJobStepTransitionException : ConflictException
{
    public InvalidProductionJobStepTransitionException(string from, string to)
        : base($"A production job step cannot move from '{from}' to '{to}'.") { }
}

public class ProductionJobNotEditableException : ConflictException
{
    public ProductionJobNotEditableException()
        : base("A production job can only be edited while it is in Draft status.") { }
}

public class ProductionJobHasNoStepsException : DomainException
{
    public ProductionJobHasNoStepsException()
        : base("A production job must have at least one step.") { }
}

public class NonOptionalStepCannotBeSkippedException : ConflictException
{
    public NonOptionalStepCannotBeSkippedException(int stepNumber)
        : base($"Step {stepNumber} is not optional and cannot be skipped.") { }
}

public class ReworkTargetInvalidException : DomainException
{
    public ReworkTargetInvalidException(int target, int from)
        : base($"Rework target step {target} must be earlier than step {from} and already completed.") { }
}

public class RoutingNotActiveForJobException : ConflictException
{
    public RoutingNotActiveForJobException(Guid routingId)
        : base($"Production routing {routingId} is not active and cannot be used for a new job.") { }
}

public class ProductionJobHasIncompleteStepsException : ConflictException
{
    public ProductionJobHasIncompleteStepsException()
        : base("All required steps must be completed before the job can be completed.") { }
}

namespace CoreAlign.Application.Tests.Idempotency;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IdempotencyTestCollection
{
    public const string Name = "Idempotency money/stock mutation handlers";

    private IdempotencyTestCollection()
    {
    }
}

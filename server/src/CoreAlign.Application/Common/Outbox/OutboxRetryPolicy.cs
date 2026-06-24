namespace CoreAlign.Application.Common.Outbox;

public interface IOutboxRetryPolicy
{
    DateTime ComputeNextAttempt(int attemptNumber, DateTime utcNow);
}

public sealed class OutboxRetryPolicy : IOutboxRetryPolicy
{
    private const double BaseSeconds = 30d;
    private const double MaxSeconds = 1800d;

    public DateTime ComputeNextAttempt(int attemptNumber, DateTime utcNow)
    {
        var exponent = Math.Max(0, attemptNumber - 1);
        var ceiling = Math.Min(MaxSeconds, BaseSeconds * Math.Pow(2, exponent));
        var jittered = ceiling * (0.5 + (Random.Shared.NextDouble() * 0.5));
        return utcNow.AddSeconds(jittered);
    }
}

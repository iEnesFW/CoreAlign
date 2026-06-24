using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime? TrialStartAtUtc { get; set; }
    public DateTime? TrialEndAtUtc { get; set; }
    public DateTime SubscriptionStartAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubscriptionEndAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;

    public bool IsTrialExpired => TrialEndAtUtc.HasValue && TrialEndAtUtc < DateTime.UtcNow;

    protected Subscription() { }

    public static Subscription CreateFreeTrial(Guid userId, int freeTrialPlanId, int trialDays)
    {
        var now = DateTime.UtcNow;
        return new Subscription
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            PlanId = freeTrialPlanId,
            Status = SubscriptionStatus.Active,
            TrialStartAtUtc = now,
            TrialEndAtUtc = now.AddDays(trialDays),
            SubscriptionStartAtUtc = now
        };
    }
}

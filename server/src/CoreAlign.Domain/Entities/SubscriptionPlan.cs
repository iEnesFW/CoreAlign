namespace CoreAlign.Domain.Entities;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int MaxUsers { get; set; } = 1;
    public int MaxProjects { get; set; } = 3;
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int TrialDurationDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    protected SubscriptionPlan() { }
}

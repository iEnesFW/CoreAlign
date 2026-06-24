using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Application.Notifications.Delivery;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    [Range(1, 100000)]
    public int PerTenantPerMinute { get; set; } = 600;

    [Range(1, 100000)]
    public int PerProviderPerMinute { get; set; } = 300;

    [Range(1, 100000)]
    public int PerRecipientPerMinute { get; set; } = 20;

    [Range(1, 50)]
    public int MaxAttempts { get; set; } = 8;
}

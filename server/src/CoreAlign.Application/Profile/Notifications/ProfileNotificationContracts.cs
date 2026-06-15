using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Profile.Notifications;

public static class ProfileNotificationKinds
{
    public const string NewOrder = "NewOrder";
    public const string OrderApproved = "OrderApproved";
    public const string OrderStatus = "OrderStatus";
    public const string OrderComment = "OrderComment";
    public const string InvoiceIssued = "InvoiceIssued";
    public const string PaymentReceived = "PaymentReceived";
    public const string DealerApprovalRequest = "DealerApprovalRequest";
    public const string ServiceTicketUpdated = "ServiceTicketUpdated";
    public const string WarrantyExpiring = "WarrantyExpiring";

    public static readonly IReadOnlyList<string> All = new[]
    {
        NewOrder,
        OrderApproved,
        OrderStatus,
        OrderComment,
        InvoiceIssued,
        PaymentReceived,
        DealerApprovalRequest,
        ServiceTicketUpdated,
        WarrantyExpiring,
    };
}

public sealed record ProfileNotificationPreferenceDto(
    string NotificationKind,
    bool EmailEnabled,
    bool InAppEnabled);

public sealed record ProfileNotificationPreferenceItem(
    string NotificationKind,
    bool EmailEnabled,
    bool InAppEnabled);

public sealed record ListProfileNotificationPreferencesQuery()
    : IRequest<IReadOnlyList<ProfileNotificationPreferenceDto>>;

public sealed record UpdateProfileNotificationPreferencesCommand(
    IReadOnlyList<ProfileNotificationPreferenceItem> Items)
    : IRequest<IReadOnlyList<ProfileNotificationPreferenceDto>>, ITransactionalRequest;

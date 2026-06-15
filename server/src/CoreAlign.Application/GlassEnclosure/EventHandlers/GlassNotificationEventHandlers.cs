using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.GlassEnclosure.EventHandlers;

internal static class NotificationContextBuilder
{
    public static async Task<(NotificationRecipient? CustomerRecipient, IReadOnlyDictionary<string, string?> Placeholders)> BuildCustomerContextAsync(
        Guid projectId,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IGlassEnclosureSettingsRepository settingsRepo,
        CancellationToken cancellationToken)
    {
        var project = await projectRepo.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return (null, EmptyPlaceholders());
        var customer = await customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);
        var settings = await settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var locale = ResolveLocale(customer, settings.DefaultLocale);
        var address = !string.IsNullOrWhiteSpace(customer?.Email) ? customer!.Email! : null;
        var recipient = address is null
            ? null
            : new NotificationRecipient(GlassNotificationRecipientKind.Customer, address, customer?.Name, locale);

        var placeholders = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["project_code"] = project.Code,
            ["project_name"] = project.ProjectName,
            ["customer_name"] = customer?.Name,
            ["grand_total"] = project.GrandTotal.ToString("F2"),
            ["currency"] = project.Currency,
            ["valid_until"] = project.ValidUntilDate?.ToString("yyyy-MM-dd"),
            ["status"] = project.Status.ToString(),
        };
        return (recipient, placeholders);
    }

    public static IReadOnlyDictionary<string, string?> EmptyPlaceholders() =>
        new Dictionary<string, string?>();

    private static string ResolveLocale(Customer? customer, string fallback)
    {
        var languageCode = customer?.LanguageCode;
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            return languageCode.Contains('-', StringComparison.OrdinalIgnoreCase)
                ? languageCode
                : $"{languageCode}-{(string.Equals(languageCode, "tr", StringComparison.OrdinalIgnoreCase) ? "TR" : "US")}";
        }
        return fallback;
    }
}

public class GlassProjectQuotedNotificationHandler : INotificationHandler<GlassProjectQuotedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly ILogger<GlassProjectQuotedNotificationHandler> _logger;

    public GlassProjectQuotedNotificationHandler(
        INotificationDispatcher dispatcher,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IGlassEnclosureSettingsRepository settingsRepo,
        ILogger<GlassProjectQuotedNotificationHandler> logger)
    {
        _dispatcher = dispatcher;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
        _logger = logger;
    }

    public async Task Handle(GlassProjectQuotedEvent notification, CancellationToken cancellationToken)
    {
        var (recipient, placeholders) = await NotificationContextBuilder.BuildCustomerContextAsync(
            notification.ProjectId, _projectRepo, _customerRepo, _settingsRepo, cancellationToken);
        if (recipient is null)
        {
            _logger.LogWarning("Skipping QuoteSent: project {ProjectId} has no customer email.", notification.ProjectId);
            return;
        }
        var enriched = new Dictionary<string, string?>(placeholders, StringComparer.OrdinalIgnoreCase)
        {
            ["share_token"] = notification.ShareToken,
            ["share_url"] = $"/share/glass/{notification.ShareToken}",
        };
        await _dispatcher.DispatchAsync(
            new NotificationDispatchRequest(
                notification.ProjectId,
                GlassNotificationEventCode.QuoteSent,
                recipient,
                GlassNotificationChannel.Email,
                enriched),
            cancellationToken);
    }
}

public class GlassProjectQuoteAcceptedNotificationHandler : INotificationHandler<GlassProjectQuoteAcceptedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public GlassProjectQuoteAcceptedNotificationHandler(
        INotificationDispatcher dispatcher,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _dispatcher = dispatcher;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task Handle(GlassProjectQuoteAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var (recipient, placeholders) = await NotificationContextBuilder.BuildCustomerContextAsync(
            notification.ProjectId, _projectRepo, _customerRepo, _settingsRepo, cancellationToken);
        if (recipient is null) return;
        await _dispatcher.DispatchAsync(
            new NotificationDispatchRequest(
                notification.ProjectId,
                GlassNotificationEventCode.QuoteAccepted,
                recipient,
                GlassNotificationChannel.Email,
                placeholders),
            cancellationToken);
    }
}

public class GlassProjectConfirmedNotificationHandler : INotificationHandler<GlassProjectConfirmedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public GlassProjectConfirmedNotificationHandler(
        INotificationDispatcher dispatcher,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _dispatcher = dispatcher;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task Handle(GlassProjectConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var (recipient, placeholders) = await NotificationContextBuilder.BuildCustomerContextAsync(
            notification.ProjectId, _projectRepo, _customerRepo, _settingsRepo, cancellationToken);
        if (recipient is null) return;
        await _dispatcher.DispatchAsync(
            new NotificationDispatchRequest(
                notification.ProjectId,
                GlassNotificationEventCode.OrderConfirmed,
                recipient,
                GlassNotificationChannel.Email,
                placeholders),
            cancellationToken);
    }
}

public class GlassWorkOrderStatusNotificationHandler : INotificationHandler<GlassWorkOrderStatusChangedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public GlassWorkOrderStatusNotificationHandler(
        INotificationDispatcher dispatcher,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _dispatcher = dispatcher;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task Handle(GlassWorkOrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.ToStatus != GlassWorkOrderStatus.Cutting &&
            notification.ToStatus != GlassWorkOrderStatus.Ready &&
            notification.ToStatus != GlassWorkOrderStatus.Installed)
        {
            return;
        }

        var (recipient, placeholders) = await NotificationContextBuilder.BuildCustomerContextAsync(
            notification.ProjectId, _projectRepo, _customerRepo, _settingsRepo, cancellationToken);
        if (recipient is null) return;

        var eventCode = notification.ToStatus switch
        {
            GlassWorkOrderStatus.Cutting => GlassNotificationEventCode.ProductionStarted,
            GlassWorkOrderStatus.Ready => GlassNotificationEventCode.ProductionCompleted,
            GlassWorkOrderStatus.Installed => GlassNotificationEventCode.InstallationCompleted,
            _ => GlassNotificationEventCode.ProductionStarted,
        };

        var smsRecipient = recipient with { Address = recipient.Address };
        await _dispatcher.DispatchAsync(
            new NotificationDispatchRequest(
                notification.ProjectId,
                eventCode,
                smsRecipient,
                GlassNotificationChannel.Sms,
                placeholders),
            cancellationToken);
    }
}

public class GlassWorkOrderDefectNotificationHandler : INotificationHandler<GlassWorkOrderDefectReportedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public GlassWorkOrderDefectNotificationHandler(
        INotificationDispatcher dispatcher,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _dispatcher = dispatcher;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task Handle(GlassWorkOrderDefectReportedEvent notification, CancellationToken cancellationToken)
    {
        var (_, placeholders) = await NotificationContextBuilder.BuildCustomerContextAsync(
            notification.ProjectId, _projectRepo, _customerRepo, _settingsRepo, cancellationToken);

        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var producerAddress = settings.NotificationEmailFrom ?? "producer@local";
        var recipient = new NotificationRecipient(
            GlassNotificationRecipientKind.Producer, producerAddress, "Producer", settings.DefaultLocale);

        var enriched = new Dictionary<string, string?>(placeholders, StringComparer.OrdinalIgnoreCase)
        {
            ["defect_notes"] = notification.DefectNotes,
        };
        await _dispatcher.DispatchAsync(
            new NotificationDispatchRequest(
                notification.ProjectId,
                GlassNotificationEventCode.ProductionStarted,
                recipient,
                GlassNotificationChannel.InApp,
                enriched),
            cancellationToken);
    }
}

using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common;
using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Application.Common.Behaviors;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.CustomerPortal.Payments;
using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.Identity.PersonaPreference;
using CoreAlign.Application.Providers.EFatura.Outbox;
using CoreAlign.Application.Providers.Payment.Outbox;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Application.Stock.Substitute;
using CoreAlign.Application.Installation;
using CoreAlign.Application.Installation.Outbox;
using CoreAlign.Application.Installation.Subscribers;
using CoreAlign.Application.Installation.Validation;
using CoreAlign.Application.Warranty;
using CoreAlign.Application.Warranty.Outbox;
using CoreAlign.Application.Warranty.Subscribers;
using CoreAlign.Domain.Events;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IFiscalYearResolver, FiscalYearResolver>();
        services.AddScoped<CoreAlign.Application.Treasury.Fx.IKnownCurrencyGuard,
            CoreAlign.Application.Treasury.Fx.KnownCurrencyGuard>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ConcurrencyTokenBehavior<,>));
        services.AddScoped<
            IPipelineBehavior<Invoices.Commands.IssueCreditNoteCommand, Invoices.DTOs.InvoiceDto>,
            IssueCreditNoteIdempotencyBehavior>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SaveChangesBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OutboxDrainBehavior<,>));

        services.AddSingleton<IOutboxRetryPolicy, OutboxRetryPolicy>();

        services.AddScoped<IPersonaPreferenceService, PersonaPreferenceService>();
        services.AddScoped<IStockAvailabilityService, StockAvailabilityService>();
        services.AddScoped<IProductSubstituteResolver, ProductSubstituteResolver>();
        services.AddScoped<CoreAlign.Application.Inventory.Services.IProductionExecutionService,
            CoreAlign.Application.Inventory.Services.ProductionExecutionService>();
        services.AddScoped<CoreAlign.Application.Inventory.Services.IStockOpeningBalanceBridge,
            CoreAlign.Application.Inventory.Services.StockOpeningBalanceBridge>();

        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        services.AddScoped<CoreAlign.Application.GlassEnclosure.Marketplace.Services.IProjectMarketplaceService,
            CoreAlign.Application.GlassEnclosure.Marketplace.Services.ProjectMarketplaceService>();
        services.AddScoped<IBOMComposer, BOMComposer>();
        services.AddScoped<IClimateAdvisor, ClimateAdvisor>();
        services.AddScoped<IFieldSurveyApplier, FieldSurveyApplier>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<CoreAlign.Application.GlassPlates.Notifications.IGlassPlateDepletionNotifier, CoreAlign.Application.GlassPlates.Notifications.GlassPlateDepletionNotifier>();
        services.AddScoped<IProductionScheduler, ProductionScheduler>();
        services.AddScoped<IProjectRecomputeService, ProjectRecomputeService>();
        services.AddScoped<IShareTokenService, ShareTokenService>();
        services.AddScoped<IThermalAcousticCalculator, ThermalAcousticCalculator>();
        services.AddScoped<IWindLoadCalculator, WindLoadCalculator>();
        services.AddSingleton<IEnclosurePreset, BalconyPreset>();
        services.AddSingleton<IEnclosurePreset, GreenhousePreset>();
        services.AddSingleton<IEnclosurePreset, ShowerCabinPreset>();
        services.AddSingleton<IEnclosurePreset, BalustradePreset>();
        services.AddSingleton<IEnclosurePreset, FramelessDoorPreset>();
        services.AddSingleton<IEnclosurePreset, CurtainWallPreset>();
        services.AddSingleton<IEnclosurePreset, SpiderFacadePreset>();
        services.AddSingleton<IEnclosurePreset, FreeFormPreset>();
        services.AddSingleton<ITemplateRegistry, TemplateRegistry>();
        services.AddSingleton<ISceneCompressor, BrotliSceneCompressor>();
        services.AddSingleton<IExpressionEvaluator, DynamicExpressoEvaluator>();
        services.AddScoped<ISceneValidator, SceneValidator>();
        services.AddScoped<ICuttingOptimizer1D, FirstFitDecreasingOptimizer1D>();
        services.AddScoped<ICuttingOptimizer2D, MaximalRectanglesOptimizer2D>();

        services.AddScoped<IGLPostingService, GLPostingService>();
        services.AddSingleton<CoreAlign.Application.Payroll.Calculation.IPayrollCalculationService,
            CoreAlign.Application.Payroll.Calculation.PayrollCalculationService>();
        services.AddScoped<IInvoicePaymentSessionWebhookService, InvoicePaymentSessionWebhookService>();
        services.AddScoped<CoreAlign.Application.CustomerPortal.Credit.ICreditLimitGuard,
            CoreAlign.Application.CustomerPortal.Credit.CreditLimitGuard>();
        services.AddScoped<CoreAlign.Application.Purchasing.ITolerancePolicyProvider,
            CoreAlign.Application.Purchasing.TolerancePolicyProvider>();

        services.AddSingleton<ISkuTemplateCache, InMemorySkuTemplateCache>();
        services.AddScoped<ISkuStrategy, DefaultSkuStrategy>();
        services.AddScoped<ICatalogProductLinker, CatalogProductLinker>();
        services.AddScoped<IBomStaleSignal, BomStaleSignal>();

        // F2 audit fix: outbox handlers for the 8 provider message types emitted
        // by the Payment + EFatura dispatchers/reconciliation jobs. Replay
        // handlers (PaymentWebhookEventHandler, EFaturaWebhookEventHandler,
        // BomRecomputedOutboxHandler) stay in Infrastructure where they were
        // wired against the inbox repository.
        // WHY these were missing: an unregistered handler is not a compile error — the processor
        // simply cannot resolve the message type, dead-letters it and moves on. GLPosting alone had
        // 58 dead-lettered journal entries in the dev database before this was found, i.e. the whole
        // outbox route into accounting was silently disconnected. OutboxHandlerRegistrationTests now
        // fails the build if any IOutboxMessageHandler in the assembly is left unregistered.
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Common.Outbox.GLPostingOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Billing.SubscriptionActivatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.EInvoice.InvoiceIssuedEInvoiceOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Auth.Handlers.SecurityAlertOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Collaboration.CommentPostedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.B2B.PortalComments.OrderCommentPostedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.B2B.DealerOrderFlow.DealerOrderSubmittedForApprovalOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.B2B.DealerOrderFlow.DealerOrderApprovedByCustomerOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.B2B.DealerOrderFlow.DealerOrderRejectedByCustomerOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, PaymentInitiatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, PaymentSucceededOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, PaymentFailedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, PaymentRefundedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, Payment3DSecureRequiredOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, PaymentWebhookReceivedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, EFaturaDispatchAttemptedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, EFaturaStatusChangedOutboxHandler>();

        // F3.3 TCMB FX sync — broadcasts FxRatesUpdatedEvent so downstream consumers
        // (dashboard invalidation, invoice cost re-derivation, etc.) can react.
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Fx.Handlers.FxRatesUpdatedOutboxHandler>();

        // F3.1 Warranty + Maintenance module: services and outbox handlers
        // for the message types emitted by the warranty/service-ticket domain.
        // INotificationHandler<T> implementations are auto-scanned by
        // AddMediatR(RegisterServicesFromAssembly) — explicit registrations
        // here would cause duplicate dispatch (each event fires twice).
        services.AddScoped<IWarrantyContractService, WarrantyContractService>();
        services.AddScoped<IServiceTicketService, ServiceTicketService>();
        services.AddScoped<IMaintenanceScheduleService, MaintenanceScheduleService>();
        services.AddScoped<IOutboxMessageHandler, WarrantyActivatedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, WarrantyExpiredOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, WarrantyExpiringNotificationOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, WarrantyExtendedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ServiceTicketOpenedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ServiceTicketResolvedOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, ServiceTicketAssignedOutboxHandler>();

        services.AddScoped<IInstallationAcceptanceService, InstallationAcceptanceService>();
        services.AddScoped<IFileOwnershipValidator, FileOwnershipValidator>();
        services.AddScoped<IOutboxMessageHandler, InstallationAcceptedOutboxHandler>();

        // F4.1 Notification subsystem (multi-channel email/sms/push/whatsapp/in-app)
        services.AddScoped<CoreAlign.Application.Notifications.INotificationDispatcher,
            CoreAlign.Application.Notifications.NotificationDispatcher>();
        services.AddScoped<CoreAlign.Application.Notifications.Templates.INotificationTemplateRenderer,
            CoreAlign.Application.Notifications.Templates.ScribanNotificationTemplateRenderer>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Notifications.Outbox.NotificationDispatchOutboxHandler>();
        services.AddScoped<CoreAlign.Application.Feedback.Notifications.IFeedbackNotificationOutbox, CoreAlign.Application.Feedback.Notifications.FeedbackNotificationOutbox>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Feedback.Notifications.FeedbackNotificationOutboxHandler>();
        services.AddScoped<IOutboxMessageHandler, CoreAlign.Application.Notifications.Delivery.NotificationChannelSendOutboxHandler>();
        services.AddScoped<CoreAlign.Application.Notifications.Delivery.INotificationDeliveryQueue,
            CoreAlign.Application.Notifications.Delivery.NotificationDeliveryQueue>();
        services.AddScoped<CoreAlign.Application.Notifications.Delivery.INotificationRateLimiter,
            CoreAlign.Application.Notifications.Delivery.NotificationRateLimiter>();
        services.AddScoped<CoreAlign.Application.Documents.Forwarding.IForwardDocumentService,
            CoreAlign.Application.Documents.Forwarding.ForwardDocumentService>();
        services.AddScoped<CoreAlign.Application.Notifications.Webhooks.INotificationStatusUpdater,
            CoreAlign.Application.Notifications.Webhooks.NotificationStatusUpdater>();

        // F4.5 Whitelabel customization (tenant theme + assets + public theme by subdomain)
        services.AddScoped<CoreAlign.Application.Whitelabel.ITenantThemeService,
            CoreAlign.Application.Whitelabel.TenantThemeService>();

        services.AddScoped<CoreAlign.Application.AiHelper.IAiHelperService,
            CoreAlign.Application.AiHelper.AiHelperService>();
        services.AddScoped<CoreAlign.Application.AiHelper.Ingestion.IKnowledgeIngestionService,
            CoreAlign.Application.AiHelper.Ingestion.KnowledgeIngestionService>();
        services.AddScoped<CoreAlign.Application.AiHelper.Ingestion.AiKbReindexJob>();

        return services;
    }
}

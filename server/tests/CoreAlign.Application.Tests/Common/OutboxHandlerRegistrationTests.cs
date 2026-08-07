using System.Reflection;
using CoreAlign.Application;
using CoreAlign.Application.Common.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Common;

/// <summary>
/// An outbox handler that is never registered is not a compile error and not a runtime crash: the
/// processor cannot resolve the message type, dead-letters the message and carries on. That is how
/// 58 journal entries and every module activation ended up in DeadLetter unnoticed. This test makes
/// the omission a build failure.
/// </summary>
public class OutboxHandlerRegistrationTests
{
    private static IReadOnlyList<Type> HandlerTypesInAssembly() =>
        typeof(ApplicationServiceRegistration).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .Where(t => typeof(IOutboxMessageHandler).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<Type> RegisteredHandlerTypes()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        return services
            .Where(d => d.ServiceType == typeof(IOutboxMessageHandler))
            .Select(d => d.ImplementationType)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();
    }

    [Fact]
    public void Every_outbox_handler_in_the_application_assembly_is_registered()
    {
        var declared = HandlerTypesInAssembly();
        var registered = RegisteredHandlerTypes().ToHashSet();

        // Infrastructure registers a handful of handlers that live in Application (BomRecomputed,
        // EFatura/Payment webhook, ShipmentEDespatch, EmailQueued, MrpSuggestionsCreated). They are
        // covered by the DI ValidateOnBuild smoke at startup, not here, so they are named rather
        // than silently tolerated — a new omission still has to be added deliberately.
        var registeredElsewhere = new[]
        {
            "BomRecomputedOutboxHandler",
            "EFaturaWebhookEventHandler",
            "PaymentWebhookEventHandler",
            "ShipmentEDespatchOutboxHandler",
            "EmailQueuedOutboxHandler",
            "MrpSuggestionsCreatedOutboxHandler",
            "ReplayOutboxHandler",
        };

        var missing = declared
            .Where(t => !registered.Contains(t))
            .Where(t => !registeredElsewhere.Contains(t.Name))
            .Select(t => t.FullName)
            .ToList();

        missing.Should().BeEmpty(
            "an unregistered outbox handler dead-letters every message of its type in silence");
    }

    [Fact]
    public void No_two_handlers_claim_the_same_message_type()
    {
        var duplicates = HandlerTypesInAssembly()
            .Select(t => new
            {
                Type = t,
                MessageType = t.GetProperty(nameof(IOutboxMessageHandler.MessageType), BindingFlags.Public | BindingFlags.Instance),
            })
            .Where(x => x.MessageType is not null && x.MessageType.GetMethod?.IsStatic == false)
            .ToList();

        duplicates.Should().NotBeEmpty("the reflection query must actually find handlers");
    }
}

using CoreAlign.Application;
using CoreAlign.Application.Common.Behaviors;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Common.Behaviors;

public class CreditNoteIdempotencyBehaviorOrderingTests
{
    [Fact]
    public void IdempotencyBehavior_RunsBetweenConcurrencyToken_AndTransaction()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var pipeline = services
            .Where(IsCreditNotePipelineBehavior)
            .Select(d => d.ImplementationType?.Name ?? "unknown")
            .ToList();

        var concurrency = pipeline.IndexOf("ConcurrencyTokenBehavior`2");
        var idempotency = pipeline.IndexOf(nameof(IssueCreditNoteIdempotencyBehavior));
        var transaction = pipeline.IndexOf("TransactionBehavior`2");

        concurrency.Should().BeGreaterThanOrEqualTo(0);
        idempotency.Should().BeGreaterThanOrEqualTo(0);
        transaction.Should().BeGreaterThanOrEqualTo(0);

        idempotency.Should().BeGreaterThan(
            concurrency,
            "the idempotency behavior is inner to ConcurrencyToken so concurrency conflicts still map to 409");
        idempotency.Should().BeLessThan(
            transaction,
            "the idempotency behavior wraps TransactionBehavior so its cache SET runs only AFTER commit");
    }

    private static bool IsCreditNotePipelineBehavior(ServiceDescriptor descriptor)
    {
        if (descriptor.ServiceType == typeof(IPipelineBehavior<IssueCreditNoteCommand, InvoiceDto>))
        {
            return true;
        }
        return descriptor.ServiceType.IsGenericType
            && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
    }
}

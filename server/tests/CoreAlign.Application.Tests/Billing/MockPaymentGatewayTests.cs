using System.Text.Json;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Infrastructure.Payments;

namespace CoreAlign.Application.Tests.Billing;

public class MockPaymentGatewayTests
{
    private readonly MockPaymentGateway _sut = new();

    [Fact]
    public async Task CreateIntent_returns_pending_with_redirect()
    {
        var request = new PaymentIntentRequest(
            OrderId: Guid.NewGuid(),
            OrderNumber: "SUB-2026-00001",
            Amount: 99m,
            Currency: "TRY",
            TenantId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            Description: "Test",
            Metadata: null);

        var result = await _sut.CreateIntentAsync(request, default);

        result.IntentId.Should().StartWith("mock_");
        result.RedirectUrl.Should().NotBeNullOrEmpty();
        result.RedirectUrl.Should().Contain(result.IntentId);
        result.Status.Should().Be(PaymentIntentStatus.Pending);
        result.RawJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleWebhook_approve_action_is_succeeded()
    {
        var payload = JsonSerializer.Serialize(new { intentId = "mock_abc", action = "approve", reference = "REF-1" });

        var result = await _sut.HandleWebhookAsync(payload, new Dictionary<string, string>(), default);

        result.IntentId.Should().Be("mock_abc");
        result.Status.Should().Be(PaymentIntentStatus.Succeeded);
        result.Reference.Should().Be("REF-1");
    }

    [Fact]
    public async Task HandleWebhook_cancel_action_is_cancelled()
    {
        var payload = JsonSerializer.Serialize(new { intentId = "mock_abc", action = "cancel" });

        var result = await _sut.HandleWebhookAsync(payload, new Dictionary<string, string>(), default);

        result.Status.Should().Be(PaymentIntentStatus.Cancelled);
    }

    [Fact]
    public async Task HandleWebhook_unknown_action_throws()
    {
        var payload = JsonSerializer.Serialize(new { intentId = "mock_abc", action = "wat" });

        var act = async () => await _sut.HandleWebhookAsync(payload, new Dictionary<string, string>(), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}

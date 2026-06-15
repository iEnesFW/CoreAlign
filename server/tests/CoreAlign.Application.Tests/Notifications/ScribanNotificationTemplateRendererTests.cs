using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Notifications.Templates;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Notifications;

public class ScribanNotificationTemplateRendererTests
{
    private readonly INotificationTemplateRepository _templates = Substitute.For<INotificationTemplateRepository>();

    private ScribanNotificationTemplateRenderer BuildSut() => new(_templates);

    [Fact]
    public async Task RenderAsync_substitutes_placeholder_values_into_body_and_subject()
    {
        var template = new NotificationTemplate(
            tenantId: null,
            key: "Warranty.Activated",
            channel: NotificationChannel.Email,
            locale: "tr",
            subject: "Hoş geldin {{customerName}}",
            bodyTemplate: "<p>Merhaba {{customerName}}, garantiniz {{warrantyNumber}} aktif.</p>");

        _templates.GetByKeyLocaleAsync(null, "Warranty.Activated", NotificationChannel.Email, "tr", Arg.Any<CancellationToken>())
            .Returns(template);

        var payload = new Dictionary<string, object?>
        {
            ["customerName"] = "Ada",
            ["warrantyNumber"] = "W-42"
        };

        var sut = BuildSut();
        var rendered = await sut.RenderAsync(null, "Warranty.Activated", NotificationChannel.Email, "tr", payload);

        rendered.Subject.Should().Be("Hoş geldin Ada");
        rendered.BodyHtml.Should().Contain("Ada");
        rendered.BodyHtml.Should().Contain("W-42");
        rendered.BodyText.Should().Contain("Ada");
        rendered.BodyText.Should().NotContain("<");
    }

    [Fact]
    public async Task RenderAsync_throws_TemplateNotFoundException_when_template_missing_for_all_locales()
    {
        _templates.GetByKeyLocaleAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);

        var sut = BuildSut();

        var act = async () => await sut.RenderAsync(null, "Missing.Key", NotificationChannel.Email, "tr", new { });

        var ex = await act.Should().ThrowAsync<TemplateNotFoundException>();
        ex.Which.Key.Should().Be("Missing.Key");
        ex.Which.Locale.Should().Be("tr");
    }

    [Fact]
    public async Task RenderAsync_falls_back_from_tr_to_en_then_throws_when_neither_exists()
    {
        var enTemplate = new NotificationTemplate(
            tenantId: null,
            key: "Payment.Succeeded",
            channel: NotificationChannel.Email,
            locale: "en",
            subject: "Payment received",
            bodyTemplate: "Thank you {{customerName}}");

        _templates.GetByKeyLocaleAsync(null, "Payment.Succeeded", NotificationChannel.Email, "tr", Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);
        _templates.GetByKeyLocaleAsync(null, "Payment.Succeeded", NotificationChannel.Email, "en", Arg.Any<CancellationToken>())
            .Returns(enTemplate);

        var sut = BuildSut();

        var rendered = await sut.RenderAsync(null, "Payment.Succeeded", NotificationChannel.Email, "tr",
            new Dictionary<string, object?> { ["customerName"] = "Grace" });

        rendered.Subject.Should().Be("Payment received");
        rendered.BodyHtml.Should().Contain("Grace");
    }

    [Fact]
    public async Task RenderAsync_throws_when_locale_chain_resolves_to_null()
    {
        _templates.GetByKeyLocaleAsync(null, "Lost.Key", NotificationChannel.Sms, "fr", Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);
        _templates.GetByKeyLocaleAsync(null, "Lost.Key", NotificationChannel.Sms, "en", Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);

        var sut = BuildSut();
        var act = async () => await sut.RenderAsync(null, "Lost.Key", NotificationChannel.Sms, "fr", new { });

        await act.Should().ThrowAsync<TemplateNotFoundException>();
    }
}

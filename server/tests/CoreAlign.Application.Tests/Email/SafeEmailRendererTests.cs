using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Email;

public class SafeEmailRendererTests
{
    private readonly SafeEmailRenderer _sut = new();

    [Fact]
    public void Renders_simple_variable_substitution()
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["firstName"] = "Ada",
        };

        var rendered = _sut.Render(
            subjectTemplate: "Hello {{ firstName }}",
            bodyTemplate: "<p>Welcome, {{ firstName }}!</p>",
            context: context);

        rendered.Subject.Should().Be("Hello Ada");
        rendered.BodyHtml.Should().Be("<p>Welcome, Ada!</p>");
    }

    [Fact]
    public void Renders_nested_object_via_dictionary_lookup()
    {
        var user = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["firstName"] = "Grace",
            ["email"] = "grace@example.com",
        };
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = user,
            ["invoiceNumber"] = "INV-2026-001",
        };

        var rendered = _sut.Render(
            subjectTemplate: "Invoice {{ invoiceNumber }} for {{ user.firstName }}",
            bodyTemplate: "<p>{{ user.firstName }} ({{ user.email }})</p>",
            context: context);

        rendered.Subject.Should().Be("Invoice INV-2026-001 for Grace");
        rendered.BodyHtml.Should().Be("<p>Grace (grace@example.com)</p>");
    }

    [Fact]
    public void Missing_variables_render_to_empty_string_without_throwing()
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["firstName"] = "Ada",
        };

        var rendered = _sut.Render(
            subjectTemplate: "Hello {{ firstName }}",
            bodyTemplate: "<p>{{ unknown }}-{{ alsoUnknown }}</p>",
            context: context);

        rendered.Subject.Should().Be("Hello Ada");
        rendered.BodyHtml.Should().Be("<p>-</p>");
    }

    [Fact]
    public void Handles_null_and_empty_template_sources()
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var rendered = _sut.Render(
            subjectTemplate: string.Empty,
            bodyTemplate: string.Empty,
            context: context);

        rendered.Subject.Should().BeEmpty();
        rendered.BodyHtml.Should().BeEmpty();
    }

    [Fact]
    public void Body_html_escapes_variable_values_to_prevent_injection()
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["userInput"] = "<script>alert('xss')</script>",
        };

        var rendered = _sut.Render(
            subjectTemplate: "Re: {{ userInput }}",
            bodyTemplate: "<p>{{ userInput }}</p>",
            context: context);

        rendered.Subject.Should().Be("Re: <script>alert('xss')</script>");
        rendered.BodyHtml.Should().NotContain("<script>");
        rendered.BodyHtml.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Ignores_unknown_directives_and_script_blocks()
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "Ada",
        };

        var rendered = _sut.Render(
            subjectTemplate: "Hi {{ name }}",
            bodyTemplate: "{{ if true }}DANGER{{ end }} {{ for x in items }}LEAK{{ end }} {{ include 'evil' }} {{ name }}",
            context: context);

        rendered.Subject.Should().Be("Hi Ada");
        rendered.BodyHtml.Should().Contain("{{ if true }}");
        rendered.BodyHtml.Should().Contain("Ada");
        rendered.BodyHtml.Should().NotContain("DANGER alert");
    }

    [Fact]
    public void Resolves_json_element_values()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("{\"order\":{\"number\":\"S-42\",\"total\":1234.5}}");
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["order"] = doc.RootElement.GetProperty("order"),
        };

        var rendered = _sut.Render(
            subjectTemplate: "Order {{ order.number }}",
            bodyTemplate: "<p>Total: {{ order.total }}</p>",
            context: context);

        rendered.Subject.Should().Be("Order S-42");
        rendered.BodyHtml.Should().Be("<p>Total: 1234.5</p>");
    }
}

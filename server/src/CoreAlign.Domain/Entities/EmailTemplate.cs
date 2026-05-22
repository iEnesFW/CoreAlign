using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Per-tenant email template. <see cref="Code"/> is the stable identifier the
/// notification dispatcher looks up (e.g. "OrderConfirmation"). Subject and
/// body can use {{Mustache}} placeholders resolved at send time. Locale lets
/// the tenant ship Turkish + English variants of the same template.
/// </summary>
public class EmailTemplate : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Locale { get; private set; } = "tr-TR";
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }
    /// <summary>JSON array of variable names supported by the template, for the UI hint list.</summary>
    public string? AvailableVariables { get; private set; }

    protected EmailTemplate() { }

    public EmailTemplate(string code, string name, string subject, string body, string locale = "tr-TR")
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));
        Code = code.Trim();
        Name = name.Trim();
        Subject = subject;
        Body = body;
        Locale = locale.Trim();
    }

    public void Update(string name, string subject, string body, string locale, string? description, string? availableVariables, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));
        Name = name.Trim();
        Subject = subject;
        Body = body;
        Locale = string.IsNullOrWhiteSpace(locale) ? "tr-TR" : locale.Trim();
        Description = description?.Trim();
        AvailableVariables = availableVariables;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

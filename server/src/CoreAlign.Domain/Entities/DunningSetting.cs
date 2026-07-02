using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class DunningSetting : TenantEntity
{
    public DunningType Type { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool SendInApp { get; private set; } = true;
    public bool SendEmail { get; private set; }

    // WHY: opaque JSON array of recipient user ids; the application layer (de)serializes — keeps the EF mapping a plain text column and the domain free of System.Text.Json.
    public string RecipientUserIdsJson { get; private set; } = "[]";

    protected DunningSetting() { }

    public DunningSetting(DunningType type, bool isEnabled, bool sendInApp, bool sendEmail, string recipientUserIdsJson)
    {
        Type = type;
        IsEnabled = isEnabled;
        SendInApp = sendInApp;
        SendEmail = sendEmail;
        RecipientUserIdsJson = string.IsNullOrWhiteSpace(recipientUserIdsJson) ? "[]" : recipientUserIdsJson;
    }

    public void Update(bool isEnabled, bool sendInApp, bool sendEmail, string recipientUserIdsJson)
    {
        IsEnabled = isEnabled;
        SendInApp = sendInApp;
        SendEmail = sendEmail;
        RecipientUserIdsJson = string.IsNullOrWhiteSpace(recipientUserIdsJson) ? "[]" : recipientUserIdsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

namespace CoreAlign.Infrastructure.Options;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Notifications:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public sealed class SendGridOptions
{
    public const string SectionName = "Notifications:SendGrid";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.sendgrid.com/v3";
}

public sealed class NetgsmSmsOptions
{
    public const string SectionName = "Notifications:Netgsm";

    public string UserCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MsgHeader { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.netgsm.com.tr";
}

public sealed class TwilioOptions
{
    public const string SectionName = "Notifications:Twilio";

    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.twilio.com/2010-04-01";
}

public sealed class FcmPushOptions
{
    public const string SectionName = "Notifications:Fcm";

    public string ServerKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://fcm.googleapis.com/fcm/send";
}

public sealed class WebPushOptions
{
    public const string SectionName = "Notifications:WebPush";

    public string VapidPublicKey { get; set; } = string.Empty;
    public string VapidPrivateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = "mailto:noreply@corealign.local";
}

public sealed class MetaWhatsAppOptions
{
    public const string SectionName = "Notifications:MetaWhatsApp";

    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com/v18.0";
}

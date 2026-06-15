namespace CoreAlign.Application.Notifications.Templates;

public sealed class TemplateNotFoundException : Exception
{
    public TemplateNotFoundException(string key, string locale)
        : base($"Notification template not found: key='{key}', locale='{locale}'")
    {
        Key = key;
        Locale = locale;
    }

    public string Key { get; }
    public string Locale { get; }
}

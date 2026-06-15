namespace CoreAlign.Application.Common.Email;

public sealed record RenderedEmail(string Subject, string BodyHtml);

public interface IEmailRenderer
{
    RenderedEmail Render(string subjectTemplate, string bodyTemplate, IReadOnlyDictionary<string, object?> context);
}

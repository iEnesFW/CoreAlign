using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class CustomerNote : TenantEntity
{
    public Guid CustomerId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;

    protected CustomerNote() { }

    public CustomerNote(Guid customerId, Guid createdByUserId, string body)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
        var trimmed = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Body is required.", nameof(body));
        if (trimmed.Length > 4000) throw new ArgumentException("Note body exceeds 4000 characters.", nameof(body));

        CustomerId = customerId;
        CreatedByUserId = createdByUserId;
        Body = trimmed;
    }
}

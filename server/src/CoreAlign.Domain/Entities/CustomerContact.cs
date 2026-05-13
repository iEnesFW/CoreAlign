using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class CustomerContact : TenantEntity
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsPrimary { get; set; }

    public Customer Customer { get; set; } = null!;

    protected CustomerContact() { }

    public CustomerContact(Guid customerId, string name)
    {
        CustomerId = customerId;
        Name = name;
    }

    public void Update(string name, string? role, string? email, string? phone, string? notes, bool isPrimary)
    {
        Name = name;
        Role = role;
        Email = email;
        Phone = phone;
        Notes = notes;
        IsPrimary = isPrimary;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

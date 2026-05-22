namespace CoreAlign.Domain.Enums;

public enum VendorStatus
{
    Active = 1,
    Blocked = 2,
    Archived = 3,
    /// <summary>Vendor created but not yet approved by purchasing manager — POs cannot reference an unapproved vendor.</summary>
    PendingApproval = 4,
}

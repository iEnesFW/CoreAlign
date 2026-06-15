using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Tenant-level override binding a <see cref="GLPostingKey"/> to a concrete GL
/// account code. Rows exist only where a tenant has customized the default;
/// auto-posting falls back to the standard TDHP code for any unmapped key.
/// </summary>
public class GLPostingMapping : TenantEntity
{
    public GLPostingKey PostingKey { get; private set; }
    public string AccountCode { get; private set; } = string.Empty;

    protected GLPostingMapping() { }

    public GLPostingMapping(GLPostingKey postingKey, string accountCode)
    {
        PostingKey = postingKey;
        SetAccountCode(accountCode);
    }

    public void SetAccountCode(string accountCode)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
        {
            throw new ArgumentException("Account code is required.", nameof(accountCode));
        }
        AccountCode = accountCode.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

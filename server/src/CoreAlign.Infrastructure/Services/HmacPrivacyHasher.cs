using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Privacy;
using Microsoft.Extensions.Configuration;

namespace CoreAlign.Infrastructure.Services;

public class HmacPrivacyHasher : IPrivacyHasher
{
    private readonly byte[] _baseSecret;

    public HmacPrivacyHasher(IConfiguration configuration)
    {
        var secret = configuration["Privacy:AuditSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Privacy:AuditSecret is not configured. Set via user-secrets or environment variable; never commit it to appsettings.json.");
        }
        _baseSecret = Encoding.UTF8.GetBytes(secret);
    }

    public string Hash(Guid tenantId, string? value)
    {
        var key = DeriveTenantKey(tenantId);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(hash);
    }

    private byte[] DeriveTenantKey(Guid tenantId)
    {
        var tenantBytes = tenantId.ToByteArray();
        var combined = new byte[_baseSecret.Length + tenantBytes.Length];
        Buffer.BlockCopy(_baseSecret, 0, combined, 0, _baseSecret.Length);
        Buffer.BlockCopy(tenantBytes, 0, combined, _baseSecret.Length, tenantBytes.Length);
        return SHA256.HashData(combined);
    }
}

using System.Security.Cryptography;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface IShareTokenService
{
    string GenerateToken();
}

public class ShareTokenService : IShareTokenService
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

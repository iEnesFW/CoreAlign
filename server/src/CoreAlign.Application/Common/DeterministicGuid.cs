using System.Security.Cryptography;
using System.Text;

namespace CoreAlign.Application.Common;

/// <summary>
/// Derives a stable GUID from a string key. Used to give events that lack a
/// natural persistent id (e.g. an order-line scrap) a deterministic idempotency
/// key, so a retried command resolves to the same key and dedupes instead of
/// double-posting.
/// </summary>
public static class DeterministicGuid
{
    public static Guid From(string input)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(input), hash);
        return new Guid(hash[..16]);
    }
}

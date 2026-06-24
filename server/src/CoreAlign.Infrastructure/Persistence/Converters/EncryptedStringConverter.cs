using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CoreAlign.Infrastructure.Persistence.Converters;

public sealed class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IDataProtector protector)
        : base(
            v => v == null ? null : protector.Protect(v),
            v => v == null ? null : protector.Unprotect(v))
    {
    }
}

public sealed class RequiredEncryptedStringConverter : ValueConverter<string, string>
{
    public RequiredEncryptedStringConverter(IDataProtector protector)
        : base(
            v => protector.Protect(v ?? string.Empty),
            v => protector.Unprotect(v ?? string.Empty))
    {
    }
}

public sealed class ResilientEncryptedStringConverter : ValueConverter<string?, string?>
{
    public ResilientEncryptedStringConverter(IDataProtector protector)
        : base(
            v => v == null ? null : protector.Protect(v),
            v => ResilientFieldDecryption.DecryptOrPassthrough(protector, v))
    {
    }
}

public sealed class RequiredResilientEncryptedStringConverter : ValueConverter<string, string>
{
    public RequiredResilientEncryptedStringConverter(IDataProtector protector)
        : base(
            v => protector.Protect(v ?? string.Empty),
            v => ResilientFieldDecryption.DecryptOrPassthrough(protector, v) ?? string.Empty)
    {
    }
}

internal static class ResilientFieldDecryption
{
    public static string? DecryptOrPassthrough(IDataProtector protector, string? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return protector.Unprotect(value);
        }
        catch (CryptographicException)
        {
            return value;
        }
        catch (FormatException)
        {
            return value;
        }
    }
}

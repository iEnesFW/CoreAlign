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

namespace CoreAlign.Application.Payroll.Employees;

internal static class PiiMasking
{
    public static string? MaskNationalId(string? nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId)) return null;
        var value = nationalId.Trim();
        if (value.Length <= 5) return new string('*', value.Length);
        var prefix = value[..3];
        var suffix = value[^2..];
        return $"{prefix}{new string('*', value.Length - 5)}{suffix}";
    }

    public static string? MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return null;
        var value = iban.Trim();
        if (value.Length <= 8) return new string('*', value.Length);
        var prefix = value[..4];
        var suffix = value[^4..];
        return $"{prefix}{new string('*', value.Length - 8)}{suffix}";
    }
}

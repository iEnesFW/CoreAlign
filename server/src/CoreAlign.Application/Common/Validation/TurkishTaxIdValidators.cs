namespace CoreAlign.Application.Common.Validation;

public static class TurkishTaxIdValidators
{
    public static bool IsValidVkn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var digits = value.Trim();
        if (digits.Length != 10) return false;
        for (var i = 0; i < 10; i++)
        {
            if (digits[i] < '0' || digits[i] > '9') return false;
        }

        var d = new int[10];
        for (var i = 0; i < 10; i++) d[i] = digits[i] - '0';

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var tmp = (d[i] + (9 - i)) % 10;
            var pow = (tmp * (1 << (9 - i))) % 9;
            if (tmp != 0 && pow == 0) pow = 9;
            sum += pow;
        }
        var check = (10 - (sum % 10)) % 10;
        return check == d[9];
    }

    public static bool IsValidTckn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var digits = value.Trim();
        if (digits.Length != 11) return false;
        for (var i = 0; i < 11; i++)
        {
            if (digits[i] < '0' || digits[i] > '9') return false;
        }
        if (digits[0] == '0') return false;

        var d = new int[11];
        for (var i = 0; i < 11; i++) d[i] = digits[i] - '0';

        var oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
        var evenSum = d[1] + d[3] + d[5] + d[7];

        var check10 = ((oddSum * 7) - evenSum) % 10;
        if (check10 < 0) check10 += 10;
        if (check10 != d[9]) return false;

        var first10Sum = 0;
        for (var i = 0; i < 10; i++) first10Sum += d[i];
        var check11 = first10Sum % 10;
        return check11 == d[10];
    }
}

using System.Collections.Concurrent;
using System.Globalization;

namespace WellnessPlatform.Services;

public static class CurrencyAmountFormatter
{
    private static readonly ConcurrentDictionary<string, int> DecimalDigitsByCurrency =
        new(StringComparer.OrdinalIgnoreCase);

    public static string FormatMinorUnits(long amountMinor, string currency)
    {
        var decimalDigits = GetCurrencyDecimalDigits(currency);
        var divisor = GetDivisor(decimalDigits);
        var amount = amountMinor / divisor;
        return $"{currency.ToUpperInvariant()} {amount.ToString($"N{decimalDigits}", CultureInfo.InvariantCulture)}";
    }

    public static long ToMinorUnits(decimal amount, string currency)
    {
        var decimalDigits = GetCurrencyDecimalDigits(currency);
        var minorAmount = amount * GetDivisor(decimalDigits);
        if (minorAmount != decimal.Truncate(minorAmount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "The amount contains more decimal places than the currency supports.");
        }

        return decimal.ToInt64(minorAmount);
    }

    private static int GetCurrencyDecimalDigits(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) ||
            currency.Length != 3 ||
            !currency.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("A three-letter ISO currency code is required.", nameof(currency));
        }

        return DecimalDigitsByCurrency.GetOrAdd(
            currency,
            FindCurrencyDecimalDigits);
    }

    private static int FindCurrencyDecimalDigits(string currency)
    {
        foreach (var culture in CultureInfo.GetCultures(
                     CultureTypes.SpecificCultures))
        {
            var region = new RegionInfo(culture.Name);
            if (string.Equals(
                    region.ISOCurrencySymbol,
                    currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                var decimalDigits = culture.NumberFormat.CurrencyDecimalDigits;
                if (decimalDigits is >= 0 and <= 3)
                {
                    return decimalDigits;
                }
            }
        }

        throw new InvalidOperationException(
            $"Currency precision is unavailable for '{currency}'.");
    }

    private static decimal GetDivisor(int decimalDigits)
    {
        var divisor = 1m;
        for (var index = 0; index < decimalDigits; index++)
        {
            divisor *= 10m;
        }

        return divisor;
    }
}

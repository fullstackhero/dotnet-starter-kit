namespace FSH.Framework.Quota;

internal static class QuotaPeriodValidator
{
    public static void Validate(int year, int month)
    {
        if (year is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }
    }
}

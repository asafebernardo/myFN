namespace MyFn.Domain.Common;

public readonly record struct FinancialMonth(int Year, int Month, int StartDay)
{
    public DateOnly Start { get; } = ComputeStart(Year, Month, StartDay);

    public DateOnly End { get; } = ComputeStart(Year, Month, StartDay).AddMonths(1).AddDays(-1);

    public string Label => Start.ToString("MMMM/yyyy", new System.Globalization.CultureInfo("pt-BR"));

    public string ShortLabel => Start.ToString("MMM/yyyy", new System.Globalization.CultureInfo("pt-BR"));

    public FinancialMonth AddMonths(int months)
    {
        var cursor = new DateOnly(Year, Month, 1).AddMonths(months);
        return new FinancialMonth(cursor.Year, cursor.Month, StartDay);
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public static FinancialMonth FromDate(DateOnly date, int startDay)
    {
        startDay = Math.Clamp(startDay, 1, 28);
        if (date.Day >= startDay)
        {
            return new FinancialMonth(date.Year, date.Month, startDay);
        }

        var previous = new DateOnly(date.Year, date.Month, 1).AddMonths(-1);
        return new FinancialMonth(previous.Year, previous.Month, startDay);
    }

    public static FinancialMonth Current(int startDay, DateOnly? today = null) =>
        FromDate(today ?? DateOnly.FromDateTime(DateTime.Today), startDay);

    private static DateOnly ComputeStart(int year, int month, int startDay)
    {
        startDay = Math.Clamp(startDay, 1, 28);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var day = Math.Min(startDay, daysInMonth);
        return new DateOnly(year, month, day);
    }
}

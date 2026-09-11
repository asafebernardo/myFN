using System.Globalization;

namespace MyFn.Web;

public static class Br
{
    private static readonly CultureInfo Pt = new("pt-BR");

    public static string Money(decimal value) => value.ToString("C", Pt);

    public static string SignedMoney(decimal value)
    {
        var formatted = Money(Math.Abs(value));
        return value < 0 ? $"-{formatted}" : formatted;
    }

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Pt);

    public static string Percent(decimal value) => value.ToString("N2", Pt) + "%";

    public static string Month(DateOnly date) => date.ToString("MMMM/yyyy", Pt);

    public static void ParseMonth(string? value, ref int year, ref int month)
    {
        if (value is { Length: >= 7 } && int.TryParse(value[..4], out var y) && int.TryParse(value.AsSpan(5, 2), out var m))
        {
            year = y;
            month = m;
        }
    }
}

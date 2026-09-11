namespace MyFn.Domain.Common;

public static class Money
{
    public const int Scale = 2;

    public static decimal Round(decimal value) =>
        decimal.Round(value, Scale, MidpointRounding.AwayFromZero);

    public static decimal Zero => 0.00m;

    public static bool IsPositive(decimal value) => value > 0;

    public static decimal ClampNonNegative(decimal value) => value < 0 ? Zero : Round(value);
}

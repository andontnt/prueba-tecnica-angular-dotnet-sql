namespace BonusTest.Domain.Common;

public static class Money
{
    /// <summary>
    /// Redondea a 2 decimales con el medio hacia arriba: 0.125 -> 0.13.
    /// </summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

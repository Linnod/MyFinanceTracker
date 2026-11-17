namespace MyFinanceTracker.Domain.Entities;

public static class FinancialRules
{
    public const int MinAllowedYear = 1900;
    public const int MaxAllowedYear = 2100;
    public const int MaxAmountDecimalPlaces = 2;

    public static bool HasValidAmountPrecision(decimal amount) =>
        decimal.Round(amount, MaxAmountDecimalPlaces) == amount;
}

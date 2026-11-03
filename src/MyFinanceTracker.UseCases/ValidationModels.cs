namespace MyFinanceTracker.UseCases;

public sealed record ValidationErrorItem(
    string ErrorCode,
    string? Suggestion = null);

public static class ValidationErrorCode
{
    public static class Transaction
    {
        public const string CategoryRequired = nameof(CategoryRequired);
        public const string CategoryNotFound = nameof(CategoryNotFound);
        public const string DateRequired = nameof(DateRequired);
        public const string DateOutOfRange = nameof(DateOutOfRange);
        public const string AmountRequired = nameof(AmountRequired);
        public const string AmountMustBePositive = nameof(AmountMustBePositive);
    }
}

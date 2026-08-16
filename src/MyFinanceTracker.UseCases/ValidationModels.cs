namespace MyFinanceTracker.UseCases;

public sealed record ValidationErrorItem(
    string ErrorCode,
    string? Suggestion = null);

public static class ValidationErrorCode
{
    public static class Common
    {
        public const string Required = nameof(Required);
        public const string MustBePositive = nameof(MustBePositive);
        public const string DateOutOfRange = nameof(DateOutOfRange);
    }

    public static class Transaction
    {
        public const string CategoryNotFound = nameof(CategoryNotFound);
        public const string CategoryRequired = nameof(CategoryRequired);
    }
}
namespace MyFinanceTracker.UseCases.Transaction.Delete;

public abstract record DeleteTransactionsResponse
{
    private DeleteTransactionsResponse() { }

    public sealed record Success(string CategoryName, DateOnly Date) : DeleteTransactionsResponse
    {
        public override string ToString() => $"Cleared: {CategoryName} for {Date:dd.MM.yyyy}";
    }

    public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors) : DeleteTransactionsResponse
    {
        public static ValidationError FromSingle(string property, string message, string? suggestion = null) 
            => new([new ValidationErrorItem(property, message, suggestion)]);

        public override string ToString() =>
            Errors.Count == 0
                ? "No specific errors"
                : string.Join("; ", Errors.Select(e => $"{e.PropertyName}: {e.Message}"));
    }

    public sealed record Failure() : DeleteTransactionsResponse;
}
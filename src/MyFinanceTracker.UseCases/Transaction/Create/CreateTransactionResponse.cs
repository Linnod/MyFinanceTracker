namespace MyFinanceTracker.UseCases.Transaction.Create;

public abstract record CreateTransactionResponse
{
    private CreateTransactionResponse() { }

    public sealed record Success(
        string CategoryName,
        IReadOnlyCollection<decimal> Amounts,
        DateOnly Date,
        string? Note
    ) : CreateTransactionResponse
    {
        public override string ToString() =>
            $"Category: {CategoryName}, Items: {Amounts.Count}, Date: {Date}";
    }

    public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors) : CreateTransactionResponse
    {
        public static ValidationError FromSingle(string property, string message, string? suggestion = null) 
            => new([new ValidationErrorItem(property, message, suggestion)]);
        public override string ToString() => 
            Errors.Count == 0 
                ? "No specific errors" 
                : string.Join("; ", Errors.Select(e => $"{e.PropertyName}: {e.Message}"));
    }

    public sealed record Failure() : CreateTransactionResponse;
}
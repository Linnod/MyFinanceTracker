using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public abstract record CreateTransactionsResponse
{
    private CreateTransactionsResponse() { }

    public sealed record Success(
        IReadOnlyList<Domain.Entities.Transaction> Transactions
    ) : CreateTransactionsResponse
    {
        public override string ToString() => Transactions.Count switch
        {
            0 => "No transactions created",
            1 => $"Created transaction for '{Transactions[0].Category.Name}' ({Transactions[0].Amount})",
            _ => $"Created {Transactions.Count} transactions (Total: {Transactions.Sum(t => t.Amount)})"
        };
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : CreateTransactionsResponse
    {
        public override string ToString() => Errors.Count == 0 
            ? "No validation errors" 
            : string.Join("; ", Errors.Select(e => $"{e.ErrorCode}"));
    }

    public sealed record Failure() : CreateTransactionsResponse
    {
        public override string ToString() => "Failed to process transactions";
    }
}
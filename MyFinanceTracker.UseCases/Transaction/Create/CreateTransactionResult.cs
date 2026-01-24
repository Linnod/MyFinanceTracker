namespace MyFinanceTracker.UseCases.Transaction.Create;

public abstract record CreateTransactionResult
{
    private CreateTransactionResult() { }

    public sealed record Success : CreateTransactionResult;
    public sealed record Failure(string Message) : CreateTransactionResult;
}
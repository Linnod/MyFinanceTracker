namespace MyFinanceTracker.UseCases.Transaction.Create;

public record CreateTransactionResult
{
    private readonly string? _errorMessage;

    public bool IsSuccess { get; }

    private CreateTransactionResult(bool success, string? error)
        => (IsSuccess, _errorMessage) = (success, error);

    public static CreateTransactionResult Success() => new(true, null);
    public static CreateTransactionResult Failure(string error) => new(false, error);

    public void Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
        {
             onSuccess();
        }
        else
        {
            onFailure(_errorMessage!);
        }
    }

    public T Match<T>(Func<T> onSuccess, Func<string, T> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(_errorMessage!);
    }
}
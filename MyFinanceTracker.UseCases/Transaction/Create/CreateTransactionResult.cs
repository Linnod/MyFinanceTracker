using System.Diagnostics;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public abstract record CreateTransactionResult
{
    private CreateTransactionResult() { }

    public sealed record SuccessResult : CreateTransactionResult;
    public sealed record FailureResult(string Message) : CreateTransactionResult;

    public static SuccessResult Success() => new();
    public static FailureResult Failure(string message) => new(message);
    public void Switch(Action onSuccess, Action<string> onFailure)
    {
        if (this is SuccessResult) onSuccess();
        else onFailure(((FailureResult)this).Message);
    }

    public T Match<T>(Func<T> onSuccess, Func<string, T> onFailure) => this switch
    {
        SuccessResult => onSuccess(),
        FailureResult f => onFailure(f.Message),
        _ => throw new UnreachableException()
    };
}
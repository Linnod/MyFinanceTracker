namespace MyFinanceTracker.Interactions.Contracts;

public record InteractionResult
{
    private InteractionResult(bool isSuccess, FinancialOperation? operation, string? error)
    {
        IsSuccess = isSuccess;
        Operation = operation;
        ErrorMessage = error;
    }

    private bool IsSuccess { get; }
    private FinancialOperation? Operation { get; }
    private string? ErrorMessage { get; }

    internal static InteractionResult Success(FinancialOperation op) => new(true, op, null);
    internal static InteractionResult Failure(string error) => new(false, null, error);

    public void Match(Action<FinancialOperation> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(Operation!);
        }
        else
        {
            onFailure(ErrorMessage!);
        }
    }
}
namespace MyFinanceTracker.Interactions.Contracts;

public abstract record InteractionResult
{
    private InteractionResult() { } 

    public sealed record Success(FinancialOperation Operation) : InteractionResult;
    public sealed record ParseError(string RawInput, string Details) : InteractionResult;
    public sealed record LogicError(string Message) : InteractionResult;
    public sealed record SystemError(string Message, Exception Exception) : InteractionResult;
}
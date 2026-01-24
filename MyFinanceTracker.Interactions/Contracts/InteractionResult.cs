namespace MyFinanceTracker.Interactions.Contracts;

public abstract record InteractionResult
{
    private InteractionResult() { }

    public sealed record Success(FinancialOperation Operation) : InteractionResult;
    public sealed record ParseError(string RawInput, string Details) : InteractionResult;
    public sealed record LogicError(string Message) : InteractionResult;
    public sealed record SystemError(string Message, Exception? Exception = null) : InteractionResult;

    public TResult Match<TResult>(
        Func<Success, TResult> onSuccess,
        Func<ParseError, TResult> onParseError,
        Func<LogicError, TResult> onLogicError,
        Func<SystemError, TResult> onSystemError)
    {
        return this switch
        {
            Success s => onSuccess(s),
            ParseError e => onParseError(e),
            LogicError e => onLogicError(e),
            SystemError e => onSystemError(e),
            _ => throw new InvalidOperationException("Unhandled interaction result type")
        };
    }

    public void Match(
        Action<Success> onSuccess,
        Action<ParseError> onParseError,
        Action<LogicError> onLogicError,
        Action<SystemError> onSystemError)
    {
        switch (this)
        {
            case Success s: onSuccess(s); break;
            case ParseError e: onParseError(e); break;
            case LogicError e: onLogicError(e); break;
            case SystemError e: onSystemError(e); break;
        }
    }
}
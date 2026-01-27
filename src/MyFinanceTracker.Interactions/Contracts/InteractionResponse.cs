namespace MyFinanceTracker.Interactions.Contracts;

public abstract record InteractionResponse
{
    private InteractionResponse() { }

    public sealed record Success(
        string InteractionDescription,
        string PrimaryValue,
        IReadOnlyCollection<ResponseDetail> Details
    ) : InteractionResponse;

    public sealed record UnrecognizedInteraction(string RawInput) : InteractionResponse;

    public sealed record InvalidInput(string InteractionDescription, string Details) : InteractionResponse;

    public sealed record LogicError(string Message) : InteractionResponse;

    public sealed record SystemError(string Message, Exception? Exception = null) : InteractionResponse;
}

public record ResponseDetail(string Name, string Value, string? Icon = null);